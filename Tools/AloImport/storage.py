"""Durable job records and local-machine process/lock identity."""
import contextlib
import ctypes
import hashlib
import json
import os
from pathlib import Path
import time
import uuid


class PipelineError(Exception):
    def __init__(self, code, summary, exit_code=3):
        super().__init__(summary)
        self.code = code
        self.exit_code = exit_code


def canonical(path):
    return os.path.normcase(str(Path(path).resolve()))


def digest(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, separators=(',', ':'),
                                     allow_nan=False).encode()).hexdigest()


def file_hash(path):
    with open(path, 'rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def read_json(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def write_json(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(path.name + '.' + uuid.uuid4().hex + '.tmp')
    try:
        with temporary.open('w', encoding='utf-8', newline='\n') as stream:
            json.dump(value, stream, sort_keys=True, indent=2, allow_nan=False)
            stream.write('\n')
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary, path)
    finally:
        temporary.unlink(missing_ok=True)


@contextlib.contextmanager
def exclusive_lock(path, wait_seconds=30):
    """OS-backed exclusion; file presence or heartbeat age never grants ownership."""
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open('a+b') as stream:
        if stream.seek(0, 2) == 0:
            stream.write(b'\0')
            stream.flush()
        deadline = time.monotonic() + wait_seconds
        while True:
            stream.seek(0)
            try:
                if os.name == 'nt':
                    import msvcrt
                    msvcrt.locking(stream.fileno(), msvcrt.LK_NBLCK, 1)
                else:
                    import fcntl
                    fcntl.flock(stream, fcntl.LOCK_EX | fcntl.LOCK_NB)
                break
            except (OSError, BlockingIOError):
                if time.monotonic() >= deadline:
                    raise PipelineError('EDITOR_BUSY', f'Lock is owned: {path}')
                time.sleep(0.05)
        try:
            yield
        finally:
            stream.seek(0)
            if os.name == 'nt':
                import msvcrt
                msvcrt.locking(stream.fileno(), msvcrt.LK_UNLCK, 1)
            else:
                import fcntl
                fcntl.flock(stream, fcntl.LOCK_UN)


def process_start(pid):
    """Stable process creation identity, including protection against PID reuse."""
    if os.name != 'nt':
        try:
            fields = Path(f'/proc/{pid}/stat').read_text().rsplit(')', 1)[1].split()
            return None if fields[0] == 'Z' else fields[19]
        except FileNotFoundError:
            return None
    kernel = ctypes.WinDLL('kernel32', use_last_error=True)
    kernel.OpenProcess.restype = ctypes.c_void_p
    kernel.OpenProcess.argtypes = [ctypes.c_ulong, ctypes.c_int, ctypes.c_ulong]
    kernel.GetProcessTimes.argtypes = [ctypes.c_void_p] + [ctypes.c_void_p] * 4
    kernel.GetExitCodeProcess.argtypes = [ctypes.c_void_p, ctypes.c_void_p]
    kernel.CloseHandle.argtypes = [ctypes.c_void_p]
    handle = kernel.OpenProcess(0x1000, False, pid)
    if not handle:
        error = ctypes.get_last_error()
        if error == 87:
            return None
        raise PipelineError('PROCESS_IDENTITY_UNAVAILABLE', f'Cannot inspect PID {pid}: Windows error {error}', 4)
    try:
        exit_code = ctypes.c_ulong()
        if not kernel.GetExitCodeProcess(handle, ctypes.byref(exit_code)):
            raise PipelineError('PROCESS_IDENTITY_UNAVAILABLE', f'Cannot inspect state for PID {pid}', 4)
        if exit_code.value != 259:
            return None
        values = [ctypes.c_ulonglong() for _ in range(4)]
        if not kernel.GetProcessTimes(handle, *(ctypes.byref(v) for v in values)):
            raise PipelineError('PROCESS_IDENTITY_UNAVAILABLE', f'Cannot read creation time for PID {pid}', 4)
        return str(values[0].value)
    finally:
        kernel.CloseHandle(handle)


def owner():
    return {'pid': os.getpid(), 'processStart': process_start(os.getpid()),
            'sessionNonce': uuid.uuid4().hex}


def is_alive(identity):
    return process_start(identity['pid']) == identity['processStart']
