﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using VirtualDesktop.VirtualDesktop;

namespace WinJump {
    internal static class Program {
        [STAThread]
        public static void Main() {
            var killExplorer = Process.Start("cmd.exe", "/c taskkill /f /im explorer.exe");

            killExplorer?.WaitForExit();

            VirtualDesktopWrapper vdw = VirtualDesktopManager.Create();

            var thread = new STAThread();

            // Start a thread that can handle UI requests
            KeyboardHook hook = new KeyboardHook();

            hook.KeyPressed += (sender, args) => {
                if (args.Key < Keys.D0 || args.Key > Keys.D9 || args.Modifier != ModifierKeys.Win) return;
                
                int index = args.Key == Keys.D0 ? 10 : (args.Key - Keys.D1);

                thread.Invoke(new Action(() => {
                    vdw.JumpTo(index);
                }));
            };
            
            for(var key = Keys.D0; key <= Keys.D9; key++) {
                hook.RegisterHotKey(ModifierKeys.Win, key);
            }
            
            Process.Start(Environment.SystemDirectory + "\\..\\explorer.exe");
            
            AppDomain.CurrentDomain.ProcessExit += (s, e) => 
            {
                hook.Dispose();
            };
            
            Application.Run();
        }
        
        // Credit https://stackoverflow.com/a/21684059/4779937
        private sealed class STAThread : IDisposable {
            public STAThread() {
                using (mre = new ManualResetEvent(false)) {
                    var thread = new Thread(() => {
                        Application.Idle += Initialize;
                        Application.Run();
                    }) {
                        IsBackground = true
                    };
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    mre.WaitOne();
                }
            }
            public void BeginInvoke(Delegate dlg, params Object[] args) {
                if (ctx == null) throw new ObjectDisposedException("STAThread");
                ctx.Post((_) => dlg.DynamicInvoke(args), null);
            }
            public object Invoke(Delegate dlg, params Object[] args) {
                if (ctx == null) throw new ObjectDisposedException("STAThread");
                object result = null;
                ctx.Send((_) => result = dlg.DynamicInvoke(args), null);
                return result;
            }

            private void Initialize(object sender, EventArgs e) {
                ctx = SynchronizationContext.Current;
                mre.Set();
                Application.Idle -= Initialize;
            }
            public void Dispose() {
                if (ctx == null) return;
                
                ctx.Send((_) => Application.ExitThread(), null);
                ctx = null;
            }

            private SynchronizationContext ctx;
            private readonly ManualResetEvent mre;
        }

    }
}