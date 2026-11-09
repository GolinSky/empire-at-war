"""Oriented source triangle comparison, independent of Blender and mesh order."""
import itertools
import math


def source_has_flat_normals(materials):
    """Recognize authored flat shading without consulting Blender's split-normal space."""
    count=0
    for slot in materials:
        indices=slot['triangles']
        for start in range(0,len(indices),3):
            triangle=indices[start:start+3]
            a,b,c=[slot['positions'][index] for index in triangle]
            ab=[b[axis]-a[axis] for axis in range(3)]
            ac=[c[axis]-a[axis] for axis in range(3)]
            cross=(ab[1]*ac[2]-ab[2]*ac[1],ab[2]*ac[0]-ab[0]*ac[2],ab[0]*ac[1]-ab[1]*ac[0])
            length=math.sqrt(sum(value*value for value in cross))
            if length<=1e-10:
                continue
            count+=1
            face=[value/length for value in cross]
            for index in triangle:
                authored=slot['normals'][index]
                normal_length=math.sqrt(sum(value*value for value in authored))
                if normal_length==0 or math.dist(face,[value/normal_length for value in authored])>.001:
                    return False
    return count>0


def compare_triangles(expected, actual, texture_size=1, ignore_normals=False):
    if len(expected)!=len(actual) or len(expected)%3:
        raise ValueError('Triangle corner count changed')
    triangles=[expected[i:i+3] for i in range(0,len(expected),3)]
    def key(triangle):
        return tuple(round(sum(c[axis] for c in triangle)/3*1000) for axis in range(3))
    buckets={}
    source_degenerate=[]
    for index,triangle in enumerate(triangles):
        buckets.setdefault(key(triangle),[]).append(index)
        ab=[triangle[1][axis]-triangle[0][axis] for axis in range(3)]
        ac=[triangle[2][axis]-triangle[0][axis] for axis in range(3)]
        area=math.sqrt(sum(value*value for value in
            (ab[1]*ac[2]-ab[2]*ac[1],ab[2]*ac[0]-ab[0]*ac[2],ab[0]*ac[1]-ab[1]*ac[0])))
        source_degenerate.append(area<=1e-10)
    used=set()
    maximum={'geometry':0,'uvPixels':0,'normal':0}
    degenerate=[]
    for start in range(0,len(actual),3):
        got=actual[start:start+3]
        centroid=key(got)
        candidates=[index for delta in itertools.product((-1,0,1),repeat=3)
            for index in buckets.get(tuple(a+b for a,b in zip(centroid,delta)),[]) if index not in used]
        if not candidates:
            raise ValueError('No original triangle matches imported topology')
        choices=[]
        closest=None
        for index in candidates:
            triangle=triangles[index]
            check_normals=not ignore_normals and not source_degenerate[index]
            for rotation in range(3):
                rotated=triangle[rotation:]+triangle[:rotation]
                errors=[{'geometry':math.dist(match[:3],corner[:3]),
                    'uvPixels':max(abs(match[axis]-corner[axis]) for axis in (3,4))*texture_size,
                    'normal':math.dist(match[5:],corner[5:]) if check_normals else 0}
                    for match,corner in zip(rotated,got)]
                # Degenerate normals neither reject candidates nor affect seam matching.
                score=sum((error['geometry']/.001)**2+(error['uvPixels']/.025)**2+
                          (error['normal']/.001)**2 for error in errors)
                if closest is None or score<closest[0]:
                    closest=(score,errors)
                if all(error['geometry']<=.001 and error['uvPixels']<=.025 and error['normal']<=.001
                       for error in errors):
                    choices.append((score,index,errors))
        if not choices:
            raise ValueError('No original oriented triangle satisfies fidelity limits: '+str(closest[1]))
        _,index,errors=min(choices,key=lambda row:row[0])
        used.add(index)
        if source_degenerate[index]:
            degenerate.append(index)
        for error in errors:
            for name,value in error.items():
                maximum[name]=max(maximum[name],value)
    return {'maxErrors':maximum,'degenerateNormalExclusions':degenerate,'normalCheck':'excluded' if ignore_normals else 'passed'}
