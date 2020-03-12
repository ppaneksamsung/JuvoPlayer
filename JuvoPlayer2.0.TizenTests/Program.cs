/*!
 *
 * [https://github.com/SamsungDForum/JuvoPlayer])
 * Copyright 2020, Samsung Electronics Co., Ltd
 * Licensed under the MIT license
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 */

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ElmSharp;
using JuvoLogger;
using JuvoLogger.Tizen;
using NUnit.Common;
using NUnitLite;
using Tizen.Applications;

namespace JuvoPlayer.TizenTests
{
    class Program : CoreUIApplication
    {
        private static ILogger Logger = LoggerManager.GetInstance().GetLogger("UT");
        private ReceivedAppControl _receivedAppControl;
        private string[] _nunitArgs;
        private Window _mainWindow;

        private static Assembly GetAssemblyByName(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == name);
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            _mainWindow = new Window("Main Window") {Geometry = new Rect(0, 0, 1920, 1080)};
            _mainWindow.Show();
        }

        private void ExtractNunitArgs()
        {
            _nunitArgs = new string[0];
            if (_receivedAppControl.ExtraData.TryGet("--nunit-args", out string unparsed))
                _nunitArgs = unparsed.Split(":");
        }

        private void RunTests(Assembly assembly)
        {
            var sb = new StringBuilder();
            var dllName = assembly.ManifestModule.ToString();

            using (var writer = new ExtendedTextWrapper(new StringWriter(sb)))
            {
                var finalNunitArgs = _nunitArgs.Concat(new[]
                    {"--result=/tmp/" + Path.GetFileNameWithoutExtension(dllName) + ".xml", "--work=/tmp"}).ToArray();
                new AutoRun(assembly).Execute(finalNunitArgs, writer, Console.In);
            }

            foreach (var line in sb.ToString().Split("\n"))
                Logger.Info(line);
        }

        private void RunJuvoPlayerTizenTests()
        {
            RunTests(typeof(Program).GetTypeInfo().Assembly);
        }

        private void RunJuvoPlayerTests()
        {
            Assembly.Load("JuvoPlayer2.0.Tests");
            RunTests(GetAssemblyByName("JuvoPlayer2.0.Tests"));
        }

        protected override async void OnAppControlReceived(AppControlReceivedEventArgs e)
        {
            _receivedAppControl = e.ReceivedAppControl;
            ExtractNunitArgs();
            await Task.Factory.StartNew(() =>
            {
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                RunJuvoPlayerTizenTests();
                RunJuvoPlayerTests();
            }, TaskCreationOptions.LongRunning);

            Exit();
        }

        private static void Main(string[] args)
        {
            TizenLoggerManager.Configure();
            var program = new Program();
            program.Run(args);
        }
    }
}