using System;using System.Linq;using System.Collections.Generic;using Mono.Cecil;using Mono.Cecil.Cil;
class Patch {
 static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> ts){foreach(var t in ts){yield return t;foreach(var n in Types(t.NestedTypes))yield return n;}}
 static void Main(string[] args){
 string dir=args[0];var resolver=new DefaultAssemblyResolver();resolver.AddSearchDirectory(dir);
 using var probe=ModuleDefinition.ReadModule(dir+"/DeepProbe.dll");var p=probe.GetType("DeepProbe");
 foreach(string file in new[]{"Assembly-CSharp.dll","UnityEngine.AssetBundleModule.dll","Unity.ResourceManager.dll","VContainer.dll"}){
 using var mod=ModuleDefinition.ReadModule(dir+"/"+file,new ReaderParameters{AssemblyResolver=resolver});
 var begin=mod.ImportReference(p.Methods.Single(x=>x.Name=="Begin"));var end=mod.ImportReference(p.Methods.Single(x=>x.Name=="End"));var endAsset=mod.ImportReference(p.Methods.Single(x=>x.Name=="EndAsset"));
 foreach(var t in Types(mod.Types))foreach(var m in t.Methods.Where(x=>x.HasBody).ToArray()){
 bool asset=t.FullName=="UnityEngine.AssetBundleRequest"&&m.Name=="get_asset";
 bool cached=t.Name=="CachedAssetService"&&m.Name=="Load";
 bool selected=(t.Name=="OrderedLifecycleCoordinator"||t.Name=="MusicService"||t.Name=="PlayerLoopRunner")&&(m.Name=="Tick"||m.Name=="LateTick"||m.Name=="Run");
 selected|=file=="Assembly-CSharp.dll"&&(m.Name=="Tick"||m.Name=="LateTick"||m.Name=="Flush");
 selected|=file=="Unity.ResourceManager.dll"&&(m.Name=="WaitForCompletionHandler"||m.Name=="Update"||m.Name=="GetAssetResult");
 if(asset||cached||selected){
 var il=m.Body.GetILProcessor();var start=new VariableDefinition(mod.TypeSystem.Int64);m.Body.Variables.Add(start);m.Body.InitLocals=true;
 var first=m.Body.Instructions[0];il.InsertBefore(first,il.Create(OpCodes.Call,begin));il.InsertBefore(first,il.Create(OpCodes.Stloc,start));
 foreach(var ret in m.Body.Instructions.Where(x=>x.OpCode==OpCodes.Ret).ToArray()){
 VariableDefinition result=null; if(m.ReturnType.MetadataType!=MetadataType.Void){result=new VariableDefinition(m.ReturnType);m.Body.Variables.Add(result);ret.OpCode=OpCodes.Stloc;ret.Operand=result;}else{ret.OpCode=OpCodes.Nop;}
 var tail=new List<Instruction>();
 if(asset)tail.Add(il.Create(OpCodes.Ldloc,result));else if(cached)tail.Add(il.Create(OpCodes.Ldarg,m.Parameters[0]));else tail.Add(il.Create(OpCodes.Ldstr,t.FullName+"."+m.Name));
 tail.Add(il.Create(OpCodes.Ldloc,start));tail.Add(il.Create(OpCodes.Call,asset?endAsset:end));if(result!=null)tail.Add(il.Create(OpCodes.Ldloc,result));tail.Add(il.Create(OpCodes.Ret));
 var at=ret;foreach(var instr in tail){il.InsertAfter(at,instr);at=instr;}
 }
 Console.WriteLine("wrapped "+m.FullName);
 }
 if(t.Name=="OptimizationAuditDriver"&&m.Name=="Boot"){
 var il=m.Body.GetILProcessor();var first=m.Body.Instructions[0];il.InsertBefore(first,il.Create(OpCodes.Call,mod.ImportReference(p.Methods.Single(x=>x.Name=="Install"))));
 }
 }
 mod.Write(dir+"/"+file+".patched");
 }
 }
}
