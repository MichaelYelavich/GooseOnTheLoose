using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace SecondHonk {
    public struct V {
        public float X,Y;
        public V(float x,float y){X=x;Y=y;}
        public float Length {get{return (float)Math.Sqrt(X*X+Y*Y);}}
        public V Unit {get{return Length<.001f?new V(1,0):this/Length;}}
        public static V operator +(V a,V b){return new V(a.X+b.X,a.Y+b.Y);}
        public static V operator -(V a,V b){return new V(a.X-b.X,a.Y-b.Y);}
        public static V operator *(V a,float b){return new V(a.X*b,a.Y*b);}
        public static V operator /(V a,float b){return new V(a.X/b,a.Y/b);}
        public static float Distance(V a,V b){return (a-b).Length;}
        public static V Direction(float angle){return new V((float)Math.Cos(angle),(float)Math.Sin(angle));}
        public static float Angle(V v){return (float)Math.Atan2(v.Y,v.X);}
        public static float Turn(float from,float to,float maximum){float d=to-from;while(d>Math.PI)d-=(float)Math.PI*2;while(d< -Math.PI)d+=(float)Math.PI*2;return from+Math.Max(-maximum,Math.Min(maximum,d));}
    }
    public sealed class SaveData {
        public int Version=2;
        public bool Sound=true,Mischief=true,RandomAttacks=false,MouseSteal=true,Companion=false,Quiet=false,ReducedMotion=false;
        public string Hat="None",Monitor="";
        public float Scale=1.5f;
    }
    public static class Store {
        public static string Root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"DesktopGoose2");
        public static string Warning="";
        public static SaveData Load(){
            string p=Path.Combine(Root,"settings.json");
            foreach(string f in new[]{p,p+".bak"})if(File.Exists(f))try{
                SaveData s=new JavaScriptSerializer().Deserialize<SaveData>(File.ReadAllText(f));
                if(s==null||s.Version!=2)throw new InvalidDataException("Unknown settings version");
                if(!new[]{"None","Scarf","Sunhat"}.Contains(s.Hat))s.Hat="None";
                if(float.IsNaN(s.Scale)||float.IsInfinity(s.Scale))s.Scale=1.5f;
                s.Scale=Math.Max(1,Math.Min(2,s.Scale));
                if(f.EndsWith(".bak"))Warning="Recovered the previous settings backup.";
                return s;
            }catch(Exception){Warning="Settings could not be read. Defaults are in use; old settings were preserved.";}
            return new SaveData();
        }
        public static bool Save(SaveData data){try{Directory.CreateDirectory(Root);string p=Path.Combine(Root,"settings.json"),temp=p+".tmp";File.WriteAllText(temp,new JavaScriptSerializer().Serialize(data));if(File.Exists(p))File.Replace(temp,p,p+".bak",true);else File.Move(temp,p);return true;}catch(Exception e){Warning=e.Message;return false;}}
    }
    public sealed class ContentPack {public string name;public string[] notes;}
    public sealed class Content {
        public readonly List<string> Notes=new List<string>(),Images=new List<string>(),Warnings=new List<string>();
        public Content(string directory){ Reload(directory); }
        public void Reload(string directory){
            Notes.Clear();Images.Clear();Warnings.Clear();
            if(Directory.Exists(directory))foreach(string path in Directory.GetFiles(directory,"*.json").Where(p=>Path.GetFileName(p)!="settings.json"&&Path.GetFileName(p)!="runtime.json").OrderBy(p=>p))try{
                if(new FileInfo(path).Length>131072)throw new InvalidDataException("Pack exceeds 128 KB");
                var pack=new JavaScriptSerializer().Deserialize<ContentPack>(File.ReadAllText(path));
                if(pack==null||pack.notes==null||pack.notes.Length>100)throw new InvalidDataException("Expected a notes array (up to 100 entries)");
                Notes.AddRange(pack.notes.Where(n=>!String.IsNullOrWhiteSpace(n)&&n.Length<=1000));
            }catch(Exception e){Warnings.Add(Path.GetFileName(path)+": "+e.Message);}
            string texts=Path.Combine(directory,"Notes");if(Directory.Exists(texts))foreach(string path in Directory.GetFiles(texts,"*.txt").OrderBy(x=>x))try{if(new FileInfo(path).Length<=65536){string text=File.ReadAllText(path);if(!String.IsNullOrWhiteSpace(text)&&text.Length<=1000)Notes.Add(text);}}catch(Exception e){Warnings.Add(Path.GetFileName(path)+": "+e.Message);}
            foreach(string name in new[]{"Pictures","Images"}){string images=Path.Combine(directory,name);if(Directory.Exists(images))foreach(string p in Directory.GetFiles(images).OrderBy(x=>x))try{if(new[]{".png",".jpg",".jpeg",".gif"}.Contains(Path.GetExtension(p).ToLowerInvariant())&&new FileInfo(p).Length<=8388608)using(var image=System.Drawing.Image.FromFile(p)){if(image.Width<=8192&&image.Height<=8192)Images.Add(p);}}catch(Exception e){Warnings.Add(Path.GetFileName(p)+": "+e.Message);}}
            if(Notes.Count==0)Notes.Add("I have stolen this window.\r\nHonk.");
        }
    }
    public enum Activity {Wander,Chase,Eat,Ball,Fetch,Return,Mud,Deliver,Sleep,Follow}
    public sealed class Goose {
        public V Pos,Velocity,Target,LeftFoot,RightFoot,FootOrigin,FootDestination;
        public float Direction,HeadDirection,Neck,FootTime,StepDuration=.2f,Wait,TaskTime;
        public int MovingFoot=-1;
        public bool FeetReady,Swimming;
        public Coating Coating;
        public int TrailRemaining;
        public Activity Task=Activity.Wander;
        public Goose(V p){Pos=Target=p;Direction=0;}
        public void Place(V p){Pos=Target=p;Velocity=new V();FeetReady=false;MovingFoot=-1;}
    }
    public enum Coating {Dry,Wet,Mud}
    public enum ToyKind {Ball,Stick,Peas,Duck,Pond,Puddle,Mud}
    public sealed class DesktopObject {
        public Guid Id=Guid.NewGuid();public ToyKind Kind;public V Pos,Size,Velocity;public bool Carried,Dragging;public float Age,Cooldown;
        public bool Surface {get{return Kind==ToyKind.Pond||Kind==ToyKind.Puddle||Kind==ToyKind.Mud;}}
        public bool Contains(V p){V d=p-Pos;return Surface?(d.X*d.X/(Size.X*Size.X*.25f)+d.Y*d.Y/(Size.Y*Size.Y*.25f)<=1):d.Length<Math.Max(Size.X,Size.Y)*.5f;}
    }
    public sealed class Mark {public V Pos;public float Age,Angle,Scale=1;public Coating Coating=Coating.Mud;}
    public sealed class World {
        public readonly SaveData Data;
        public readonly Content Content;
        public readonly Goose Goose=new Goose(new V(350,300)),Friend=new Goose(new V(270,330));
        public readonly List<Mark> Marks=new List<Mark>();
        public readonly List<DesktopObject> Objects=new List<DesktopObject>();
        public Behavior Behavior=new Behavior();
        public bool Editing,Exploding;public float DismissRemaining,ExplosionTime;
        DesktopObject ball,stick;
        readonly Random random;
        readonly Queue<int> deck=new Queue<int>();
        int noteIndex,imageIndex;
        float autoIn=20,ballCooldown,napIn=180,waitOffscreen;
        bool deliverImage;
        public float Width=1280,Height=752,Time,GrabRemaining,QuietRemaining;
        public V Pointer,ReturnPoint,GrabTarget;
        public V BallPos {get{return ball==null?new V():ball.Pos;}set{if(ball!=null)ball.Pos=value;}}
        public V BallVelocity {get{return ball==null?new V():ball.Velocity;}set{if(ball!=null)ball.Velocity=value;}}
        public V StickPos {get{return stick==null?new V():stick.Pos;}set{if(stick!=null)stick.Pos=value;}}
        public bool BallVisible {get{return ball!=null&&Objects.Contains(ball);}set{if(!value&&ball!=null){Objects.Remove(ball);ball=null;}}}
        public bool StickVisible {get{return stick!=null&&Objects.Contains(stick);}set{if(!value&&stick!=null){Objects.Remove(stick);stick=null;}}}
        public bool CarryingStick {get{return stick!=null&&stick.Carried;}set{if(stick!=null)stick.Carried=value;}}
        public bool Paused;
        public int DeliveryStage,DeliverySide,Meals,Fetches,Kicks,Deliveries;
        public string DeliveryText="",DeliveryImage="";
        public event Action Honked,FootLanded,CursorCaught,DeliveryStarted,DeliveryCompleted;
        public float Scale {get{return Data.Scale;}}
        public float GooseScale(Goose goose){return Scale*(goose==Friend?.85f:1);}
        public bool HoldingCursor {get{return GrabRemaining>0&&!Paused&&!Data.Quiet&&Data.MouseSteal;}}
        public World(SaveData data,Content content,int seed){Data=data;Content=content;random=new Random(seed);}
        public void ApplyBehavior(Behavior behavior){Behavior=behavior;deck.Clear();autoIn=behavior.PrankMinSeconds;}
        public V Clamp(V p){return new V(Math.Max(35*Scale,Math.Min(Width-35*Scale,p.X)),Math.Max(65*Scale,Math.Min(Height-20*Scale,p.Y)));}
        public V ConstrainObject(DesktopObject o,V p){if(!o.Surface)return Clamp(p);o.Size=new V(Math.Min(o.Size.X,Width-32),Math.Min(o.Size.Y,Height-32));float x=o.Size.X/2+14,y=o.Size.Y/2+14;return new V(Math.Max(x,Math.Min(Width-x,p.X)),Math.Max(y,Math.Min(Height-y,p.Y)));}
        public V Beak(Goose g){float extend=g.Neck;V forward=V.Direction(g.HeadDirection);return g.Pos+new V(0,-14*Scale)+V.Direction(g.Direction)*(15*Scale)+forward*((11+13*extend)*Scale)+new V(0,-(20-10*extend)*Scale);}
        public void Resize(float width,float height){Width=Math.Max(400,width);Height=Math.Max(300,height);Goose.Place(Clamp(Goose.Pos));Friend.Place(Clamp(Friend.Pos));ClearProps();Cancel();}
        public void ClearProps(){Cancel();Objects.Clear();ball=stick=null;Marks.Clear();Goose.Coating=Friend.Coating=Coating.Dry;Goose.TrailRemaining=Friend.TrailRemaining=0;Goose.Swimming=Friend.Swimming=false;}
        public void Cancel(){SetTask(Activity.Wander);GrabRemaining=0;}
        public void SetTask(Activity task){if(task!=Activity.Fetch&&task!=Activity.Return)foreach(var o in Objects)o.Carried=false;Goose.Task=task;Goose.TaskTime=0;Goose.Wait=0;if(task==Activity.Deliver)DeliveryStage=0;}
        public DesktopObject PlaceObject(ToyKind kind,V p){
            if(Objects.Count>=16)return null;var o=new DesktopObject{Kind=kind,Pos=Clamp(p),Size=kind==ToyKind.Pond?new V(230,145):kind==ToyKind.Puddle||kind==ToyKind.Mud?new V(135,80):kind==ToyKind.Stick?new V(48,24):kind==ToyKind.Peas?new V(32,28):kind==ToyKind.Duck?new V(48,36):new V(36,36)};o.Pos=ConstrainObject(o,p);Objects.Add(o);
            if(kind==ToyKind.Ball){ball=o;SetTask(Activity.Ball);}if(kind==ToyKind.Stick){foreach(var existing in Objects)existing.Carried=false;stick=o;ReturnPoint=Goose.Pos;SetTask(Activity.Fetch);}if(kind==ToyKind.Peas){GrabRemaining=0;SetTask(Activity.Eat);}return o;
        }
        public void RemoveObject(DesktopObject o){Objects.Remove(o);if(o==ball)ball=null;if(o==stick){stick=null;Cancel();}}
        public void Honk(){if(!Data.Quiet&&Data.Sound&&Honked!=null)Honked();}
        public void Chase(V p){if(Paused||Data.Quiet)return;Pointer=Clamp(p);GrabRemaining=0;SetTask(Activity.Chase);Honk();}
        public void Follow(V p){if(Paused)return;Pointer=Clamp(p);GrabRemaining=0;SetTask(Activity.Follow);}
        public void Call(V p){GrabRemaining=0;SetTask(Activity.Follow);Pointer=Clamp(p);Honk();}
        public void Feed(V p){if(Paused)return;PlaceObject(ToyKind.Peas,p);}
        public void ThrowBall(V p){if(Paused||Data.Quiet)return;GrabRemaining=0;var o=PlaceObject(ToyKind.Ball,p);if(o!=null)o.Velocity=new V(random.Next(-80,81),random.Next(-80,81));ballCooldown=0;Honk();}
        public void Fetch(V p){if(Paused||Data.Quiet)return;GrabRemaining=0;PlaceObject(ToyKind.Stick,p);Honk();}
        public void MakeMud(){if(Paused||Data.Quiet)return;GrabRemaining=0;Goose.Coating=Coating.Mud;Goose.TrailRemaining=Behavior.TrailSteps;SetTask(Activity.Mud);Goose.Target=RandomPoint();Honk();}
        public void Deliver(bool image){if(Paused||Data.Quiet)return;GrabRemaining=0;deliverImage=image&&Content.Images.Count>0;DeliverySide=Goose.Pos.X<Width/2?-1:1;SetTask(Activity.Deliver);Goose.Target=new V(DeliverySide<0?-75*Scale:Width+75*Scale,Math.Max(120*Scale,Math.Min(Height-100*Scale,Goose.Pos.Y)));Honk();}
        public void SetQuiet(bool quiet){Data.Quiet=quiet;QuietRemaining=0;Cancel();}
        V RandomPoint(){return Clamp(new V((float)random.NextDouble()*Width,(float)random.NextDouble()*Height));}
        void ChooseActivity(){
            if(deck.Count==0){var choices=new List<int>();int[] weights={Behavior.MudWeight,Behavior.NoteWeight,Behavior.PictureWeight,Behavior.ChaseWeight,Behavior.ToyWeight};for(int k=0;k<weights.Length;k++)for(int j=0;j<weights[k];j++)choices.Add(k);if(choices.Count==0)return;while(choices.Count>0){int i=random.Next(choices.Count);deck.Enqueue(choices[i]);choices.RemoveAt(i);}}
            switch(deck.Dequeue()){
                case 0:MakeMud();break;case 1:Deliver(false);break;case 2:Deliver(true);break;
                case 3:if(Data.RandomAttacks)Chase(Pointer);break;
                case 4:ball=Objects.Where(o=>o.Kind==ToyKind.Ball&&!o.Dragging).OrderBy(o=>V.Distance(Goose.Pos,o.Pos)).FirstOrDefault();if(BallVisible)SetTask(Activity.Ball);else if(Data.Companion){SetTask(Activity.Follow);Pointer=Friend.Pos;}break;
            }
        }
        void Move(Goose g,V target,float speed,float dt,bool offscreen){
            UpdateSurface(g);if(g.Swimming)speed*=.65f;
            if(!offscreen)target=Clamp(target);V delta=target-g.Pos;
            V desired=delta.Length>4*Scale?delta.Unit*Math.Min(speed*Scale,delta.Length*5):new V();
            V change=desired-g.Velocity;float acceleration=(g.Task==Activity.Chase?2300:1300)*Scale*dt;if(change.Length>acceleration)change=change.Unit*acceleration;
            g.Velocity+=change;g.Pos+=g.Velocity*dt;if(!offscreen)g.Pos=Clamp(g.Pos);
            UpdateSurface(g);
            if(g.Velocity.Length>6)g.Direction=V.Turn(g.Direction,V.Angle(g.Velocity),dt*3);
            float headTarget=g.Direction;if(V.Distance(Pointer,g.Pos)<140*Scale&&!HoldingCursor&&g.Task!=Activity.Deliver)headTarget=V.Turn(g.Direction,V.Angle(Pointer-g.Pos),.65f);
            g.HeadDirection=V.Turn(g.HeadDirection,headTarget,dt*5);
            float neckTarget=g.Task==Activity.Chase||g.Task==Activity.Fetch||g.Task==Activity.Return?1:0;g.Neck+=(neckTarget-g.Neck)*Math.Min(1,dt*6);
            SolveFeet(g,dt);
        }
        void SolveFeet(Goose g,float dt){
            float scale=GooseScale(g);V side=V.Direction(g.Direction+(float)Math.PI/2),leftHome=g.Pos-side*(3*scale),rightHome=g.Pos+side*(3*scale);
            if(!g.FeetReady){g.LeftFoot=leftHome;g.RightFoot=rightHome;g.FeetReady=true;g.MovingFoot=-1;}
            if(g.Swimming){g.LeftFoot=leftHome;g.RightFoot=rightHome;g.MovingFoot=-1;return;}
            if(g.MovingFoot<0){
                float ld=V.Distance(g.LeftFoot,leftHome),rd=V.Distance(g.RightFoot,rightHome);
                if(Math.Max(ld,rd)>5*scale){g.MovingFoot=ld>=rd?0:1;g.FootOrigin=g.MovingFoot==0?g.LeftFoot:g.RightFoot;g.FootDestination=(g.MovingFoot==0?leftHome:rightHome)+g.Velocity*.055f;g.FootTime=0;g.StepDuration=g.Velocity.Length>150*Scale?.1f:.2f;}
            }else{
                g.FootTime+=dt;float t=Math.Min(1,g.FootTime/g.StepDuration),ease=t<.5f?4*t*t*t:1-(float)Math.Pow(-2*t+2,3)/2;
                V foot=g.FootOrigin+(g.FootDestination-g.FootOrigin)*ease;if(g.MovingFoot==0)g.LeftFoot=foot;else g.RightFoot=foot;
                if(t>=1){LandFoot(g,foot);if(!g.Swimming&&Data.Sound&&!Data.Quiet&&FootLanded!=null)FootLanded();g.MovingFoot=-1;}
            }
        }
        public void UpdateSurface(Goose g){var surface=Objects.LastOrDefault(o=>o.Surface&&o.Contains(g.Pos));bool swimming=surface!=null&&surface.Kind==ToyKind.Pond;if(swimming!=g.Swimming){g.FeetReady=false;g.MovingFoot=-1;}g.Swimming=swimming;if(surface!=null){g.Coating=surface.Kind==ToyKind.Mud?Coating.Mud:Coating.Wet;g.TrailRemaining=Behavior.TrailSteps;}}
        public void LandFoot(Goose g,V foot){if(g.Swimming||Objects.Any(o=>o.Surface&&o.Contains(foot)))return;if(g.Coating!=Coating.Dry&&g.TrailRemaining>0){Marks.Add(new Mark{Pos=new V((float)Math.Round(foot.X),(float)Math.Round(foot.Y)),Angle=g.Direction,Scale=GooseScale(g),Coating=g.Coating});g.TrailRemaining--;if(g.TrailRemaining==0)g.Coating=Coating.Dry;while(Marks.Count>256)Marks.RemoveAt(0);}}
        public void AdvanceQuiet(float elapsed){if(QuietRemaining>0){QuietRemaining-=elapsed;if(QuietRemaining<=0)SetQuiet(false);}}
        public void Update(float dt){
            dt=Math.Max(0,Math.Min(.1f,dt));AdvanceQuiet(dt);if(!Data.MouseSteal)GrabRemaining=0;if(Paused){GrabRemaining=0;if(!Goose.FeetReady)SolveFeet(Goose,0);if(Data.Companion&&!Friend.FeetReady)SolveFeet(Friend,0);return;}Time+=dt;Goose.TaskTime+=dt;ballCooldown-=dt;
            for(int i=Marks.Count-1;i>=0;i--){Marks[i].Age+=dt;if(Marks[i].Age>=Behavior.FootprintSeconds)Marks.RemoveAt(i);}if(Marks.Count>256)Marks.RemoveRange(0,Marks.Count-256);
            foreach(var o in Objects){o.Age+=dt;o.Cooldown-=dt;if(o.Carried||o.Dragging||o.Surface)continue;o.Pos+=o.Velocity*dt;o.Velocity*=(float)Math.Exp(-1.1f*dt);V constrained=Clamp(o.Pos);if(constrained.X!=o.Pos.X)o.Velocity.X*= -.75f;if(constrained.Y!=o.Pos.Y)o.Velocity.Y*= -.75f;o.Pos=constrained;if(o.Kind==ToyKind.Duck&&Objects.Any(s=>s.Kind==ToyKind.Pond&&s.Contains(o.Pos)))o.Pos+=new V((float)Math.Sin(Time+o.Pos.Y)*dt*4,0);if(o.Kind==ToyKind.Ball||o.Kind==ToyKind.Duck)foreach(var g in Data.Companion?new[]{Goose,Friend}:new[]{Goose})if(o.Cooldown<=0&&V.Distance(g.Pos,o.Pos)<35*Scale){o.Velocity=(o.Pos-g.Pos).Unit*Behavior.NudgeSpeed*Scale;o.Cooldown=1;Kicks++;}}
            if(Data.Quiet&&(Goose.Task==Activity.Chase||Goose.Task==Activity.Mud||Goose.Task==Activity.Deliver||Goose.Task==Activity.Fetch||Goose.Task==Activity.Ball))Cancel();
            V target=Goose.Pos;float speed=80;bool offscreen=false;
            switch(Goose.Task){
                case Activity.Wander:
                    if(V.Distance(Goose.Pos,Goose.Target)<10*Scale){Goose.Wait-=dt;if(Goose.Wait<=0){Goose.Target=RandomPoint();Goose.Wait=1+ (float)random.NextDouble()*2;}}
                    target=Goose.Target;autoIn-=dt;napIn-=dt;
                    if(Objects.Any(o=>o.Kind==ToyKind.Peas))SetTask(Activity.Eat);else if(autoIn<=0){autoIn=Behavior.PrankMinSeconds+(float)random.NextDouble()*(Behavior.PrankMaxSeconds-Behavior.PrankMinSeconds);if(Data.Mischief&&!Data.Quiet)ChooseActivity();}
                    if(napIn<=0){napIn=180+random.Next(180);SetTask(Activity.Sleep);Goose.Target=new V(Width-70*Scale,Height-30*Scale);}break;
                case Activity.Chase:
                    speed=400;
                    if(HoldingCursor){GrabRemaining-=dt;target=GrabTarget;if(GrabRemaining<=0||V.Distance(Goose.Pos,target)<20*Scale)Cancel();}
                    else {target=Clamp(Pointer-(Beak(Goose)-Goose.Pos));if(Data.MouseSteal&&Goose.TaskTime<9&&V.Distance(Beak(Goose),Pointer)<18*Scale){GrabRemaining=1.4f;GrabTarget=Clamp(Goose.Pos+V.Direction((float)random.NextDouble()*6.28f)*(340*Scale));if(Data.Sound&&CursorCaught!=null)CursorCaught();}if(Goose.TaskTime>9)Cancel();}break;
                case Activity.Follow:target=Pointer;speed=160;if(Goose.TaskTime>8)Cancel();break;
                case Activity.Eat:
                    DesktopObject pea=Objects.Where(o=>o.Kind==ToyKind.Peas&&!o.Dragging).OrderBy(p=>V.Distance(p.Pos,Goose.Pos)).FirstOrDefault();if(pea==null){Cancel();break;}target=pea.Pos;speed=Behavior.ToyApproachSpeed;
                    if(V.Distance(Beak(Goose),target)<35*Scale||V.Distance(Goose.Pos,target)<12*Scale){Objects.Remove(pea);Meals++;}break;
                case Activity.Ball:
                    if(!BallVisible||Goose.TaskTime>18){ball=Objects.FirstOrDefault(o=>o.Kind==ToyKind.Ball&&!o.Dragging);Cancel();break;}if(ball.Dragging)break;target=Clamp(BallPos-(Beak(Goose)-Goose.Pos));speed=Behavior.ToyApproachSpeed;
                    if((V.Distance(Beak(Goose),BallPos)<28*Scale||V.Distance(Goose.Pos,BallPos)<24*Scale)&&ballCooldown<=0){BallVelocity=(BallPos-Goose.Pos).Unit*(260*Scale);ballCooldown=1;Kicks++;Honk();}break;
                case Activity.Fetch:if(!StickVisible){Cancel();break;}target=StickPos;speed=Behavior.ToyApproachSpeed;if(!stick.Dragging&&V.Distance(Goose.Pos,target)<25*Scale){CarryingStick=true;SetTask(Activity.Return);}break;
                case Activity.Return:target=ReturnPoint;speed=180;if(stick!=null)stick.Pos=Beak(Goose);if(V.Distance(Goose.Pos,target)<18*Scale){Fetches++;if(stick!=null){stick.Carried=false;stick.Pos=Clamp(Beak(Goose));}Cancel();Honk();}break;
                case Activity.Mud:target=Goose.Target;speed=200;if(V.Distance(Goose.Pos,target)<30*Scale)Goose.Target=RandomPoint();if(Goose.TaskTime>12)Cancel();break;
                case Activity.Deliver:
                    offscreen=true;target=Goose.Target;
                    if(DeliveryStage==0&&V.Distance(Goose.Pos,target)<20*Scale){DeliveryStage=1;waitOffscreen=.8f;}
                    else if(DeliveryStage==1){waitOffscreen-=dt;if(waitOffscreen<=0){DeliveryText=Content.Notes[noteIndex++%Content.Notes.Count];DeliveryImage=deliverImage?Content.Images[imageIndex++%Content.Images.Count]:"";DeliveryStage=2;Goose.Target=Clamp(new V(Width*(DeliverySide<0?.48f:.52f),Height*.55f));if(DeliveryStarted!=null)DeliveryStarted();}}
                    else if(DeliveryStage==2&&V.Distance(Goose.Pos,target)<15*Scale){Deliveries++;Cancel();if(DeliveryCompleted!=null)DeliveryCompleted();}break;
                case Activity.Sleep:target=Goose.Target;speed=55;if(Goose.TaskTime>25)Cancel();break;
            }
            Move(Goose,target,speed,dt,offscreen);
            if(Data.Companion){V follow=Goose.Pos-V.Direction(Goose.Direction)*(65*Scale)+new V(0,15*Scale);var snack=Objects.FirstOrDefault(o=>o.Kind==ToyKind.Peas&&!o.Dragging);if(snack!=null){follow=snack.Pos;if(V.Distance(Friend.Pos,follow)<25*Scale){Objects.Remove(snack);Meals++;}}if(V.Distance(Friend.Pos,follow)<18*Scale)follow=Friend.Pos;Move(Friend,follow,140,dt,false);}
        }
    }
}
