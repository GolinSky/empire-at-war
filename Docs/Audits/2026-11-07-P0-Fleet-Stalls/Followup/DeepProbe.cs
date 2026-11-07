using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
public sealed class DeepProbe : MonoBehaviour {
 static readonly List<string> Records=new List<string>(8192);
 static double _last; static int _gc; static string _dir;
 public static void Install() {
  _dir=Environment.GetEnvironmentVariable("EAW_CAPTURE_DIR");
  Records.Add("startUTC="+DateTime.UtcNow.ToString("O")+" stopwatch="+Stopwatch.GetTimestamp()+" frequency="+Stopwatch.Frequency);
  var go=new GameObject("Deep stall probe"); UnityEngine.Object.DontDestroyOnLoad(go);go.AddComponent<DeepProbe>();
 }
 public static long Begin(){return Stopwatch.GetTimestamp();}
 public static void EndAsset(UnityEngine.Object asset,long start){End("AssetBundleRequest.asset:"+(asset==null?"null":asset.name),start);}
 public static void End(string label,long start){
  long now=Stopwatch.GetTimestamp();double ms=(now-start)*1000d/Stopwatch.Frequency;
  if(ms<5)return;
  Records.Add("method frame="+Time.frameCount+" rt="+Time.realtimeSinceStartupAsDouble+" start="+start+" end="+now+" ms="+ms+" key="+label+"\n"+Environment.StackTrace);
  double cost=(Stopwatch.GetTimestamp()-now)*1000d/Stopwatch.Frequency;
  if(cost>1)Records.Add("probeCost frame="+Time.frameCount+" ms="+cost+" label="+label);
 }
 void Update(){
  double now=Time.realtimeSinceStartupAsDouble;int gc=GC.CollectionCount(0);
  if(_last>0&&(now-_last)*1000>50)Records.Add("frameGap frame="+Time.frameCount+" rt="+now+" ms="+((now-_last)*1000)+" gcDelta="+(gc-_gc)+" utc="+DateTime.UtcNow.ToString("O"));
  _gc=gc;_last=now;
 }
 public static void Flush(){if(_dir!=null)File.WriteAllLines(_dir+"/deep-events.txt",Records);}
 void OnApplicationQuit(){Flush();}
}
