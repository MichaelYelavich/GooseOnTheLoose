using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
namespace SecondHonk {
    static class Program {
        internal static string ArtifactsFolder {get{string root=AppDomain.CurrentDomain.BaseDirectory;return File.Exists(Path.Combine(root,"..","build.ps1"))?Path.GetFullPath(Path.Combine(root,"..","artifacts")):Path.Combine(root,"artifacts");}}
        [STAThread]static void Main(string[] args){
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            string root=AppDomain.CurrentDomain.BaseDirectory,command=args.Length>0?args[0].TrimStart('-'):"";var content=new Content(Path.Combine(root,"Packs"));
            if(command=="make-assets"){MakeAssets(root);return;}
            if(command=="self-test"){SelfTest.Run(content);return;}
            if(command=="native-smoke"){SelfTest.NativeSmoke(content);return;}
            if(command=="snapshot"){
                string target=args.Length>1?Path.GetFullPath(args[1]):Path.Combine(ArtifactsFolder,"goose.png");Directory.CreateDirectory(Path.GetDirectoryName(target));var world=new World(new SaveData(),content,42);world.Goose.Place(new V(130,145));world.Goose.Direction=world.Goose.HeadDirection=-.3f;world.Update(0);
                using(var bitmap=new Bitmap(280,210)){using(var g=Graphics.FromImage(bitmap)){g.Clear(Color.Transparent);Art.Scene(g,world);}bitmap.Save(target,System.Drawing.Imaging.ImageFormat.Png);}return;
            }
            UserContent.Initialize(root);content=new Content(Store.Root);
            if(command=="seed-content")return;
            bool created;using(var mutex=new Mutex(true,CommandRegistry.Prefix+"Instance",out created)){
                if(!created){if(command.Length==0)command="come";if(CommandRegistry.Find(command)!=null)try{using(var signal=EventWaitHandle.OpenExisting(CommandRegistry.Prefix+command))signal.Set();if(command=="quit"){bool acquired=false;try{acquired=mutex.WaitOne(5000);}catch(AbandonedMutexException){acquired=true;}if(acquired)mutex.ReleaseMutex();else Environment.ExitCode=2;}}catch(WaitHandleCannotBeOpenedException){}return;}
                if(command=="quit")return;
                Application.ThreadException+=(s,e)=>{Log(e.Exception);Application.Exit();};
                Application.Run(new DesktopSession(Store.Load(),content,command));
            }
        }
        internal static void Log(Exception error){try{Directory.CreateDirectory(Store.Root);File.AppendAllText(Path.Combine(Store.Root,"error.log"),DateTime.Now.ToString("s")+" "+error+Environment.NewLine);}catch(Exception){}}
        static void MakeAssets(string root){
            using(var icon=Native.MakeIcon())using(var stream=File.Create(Path.Combine(root,"DesktopGoose2.ico")))icon.Save(stream);
            string folder=Path.Combine(root,"Packs","Images");Directory.CreateDirectory(folder);
            using(var bitmap=new Bitmap(480,320))using(var g=Graphics.FromImage(bitmap)){
                g.Clear(Color.FromArgb(245,245,238));g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using(var font=new Font("Segoe UI",34,FontStyle.Bold,GraphicsUnit.Pixel))using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})g.DrawString("THIS COULD HAVE\nBEEN A HONK.",font,Brushes.Black,new RectangleF(24,50,432,220),format);
                bitmap.Save(Path.Combine(folder,"honk.png"),System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    }
}
