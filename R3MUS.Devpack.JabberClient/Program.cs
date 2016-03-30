using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;

using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.Collections;
using agsXMPP.protocol.iq.roster;
using R3MUS.Devpack.Slack;
using System.Text.RegularExpressions;
using System.ServiceProcess;

namespace R3MUS.Devpack.JabberClient
{
    public class JabberBot : ServiceBase
    {
        static bool _wait;

        static void Main(string[] args)
        {
            var jabberBot = new JabberBot();

            if(Environment.UserInteractive)
            {
                jabberBot.OnStart(args);
            }
            else
            {
                ServiceBase.Run(new ServiceBase[] { jabberBot });
            }
            Console.ReadLine();
        }
        
        protected override void OnStart(string[] args)
        {
            Start();
        }

        public void Start()
        {
            Console.Title = "Jabber Client";
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("Login");
            Console.WriteLine();

            Console.WriteLine(string.Format("JID: {0}", Properties.Settings.Default.XMPPUser));

            Console.WriteLine(string.Format("Password: {0}",
                Regex.Replace(Properties.Settings.Default.XMPPPassword, @"[^d]", "*")));

            Jid jidSender = new Jid(Properties.Settings.Default.XMPPUser);
            XmppClientConnection xmpp = new XmppClientConnection(jidSender.Server);
            xmpp.Open(jidSender.User, Properties.Settings.Default.XMPPPassword);

            xmpp.OnLogin += new ObjectHandler(xmpp_OnLogin);

            Console.Write("Wait for Login ");

            var i = 0;
            _wait = true;

            do
            {
                Console.Write(".");
                i++;
                if (i == 10)
                {
                    _wait = false;
                }
                Thread.Sleep(500);
            }
            while (_wait);

            Console.WriteLine("Login Status:");
            Console.WriteLine("xmpp Connection State {0}", xmpp.XmppConnectionState);
            Console.WriteLine("xmpp Authenticated? {0}", xmpp.Authenticated);
            Console.WriteLine();

            Console.WriteLine("Sending Presence");
            Presence p = new Presence(ShowType.chat, "Online");
            p.Type = PresenceType.available;
            xmpp.Send(p);
            Console.WriteLine();

            Console.WriteLine(string.Format("Listening to {0}", Properties.Settings.Default.XMPPChannel));
            Console.WriteLine();

            Console.WriteLine("Start Chat");

            xmpp.MessageGrabber.Add(new Jid(Properties.Settings.Default.XMPPChannel),
                                     new BareJidComparer(),
                                     new MessageCB(MessageCallBack),
                                     null);

            string outMessage;
            bool halt = false;
            do
            {
                Console.ForegroundColor = ConsoleColor.Green;
                outMessage = Console.ReadLine();
                if (outMessage == "q!")
                {
                    halt = true;
                }
            } while (!halt);
            Console.ForegroundColor = ConsoleColor.White;

            xmpp.Close();
        }

        private void xmpp_OnPresence(object sender, Presence pres)
        {
            Console.WriteLine("Available Contacts: ");
            Console.WriteLine("{0}@{1}  {2}", pres.From.User, pres.From.Server, pres.Type);
            Console.WriteLine();
        }

        private void xmpp_OnLogin(object sender)
        {
            _wait = false;
            Console.WriteLine("Logged In");
        }

        private void MessageCallBack(object sender,
                                    agsXMPP.protocol.client.Message msg,
                                    object data)
        {
            if (msg.Body != null)
            {
                var lines = msg.Body.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var senderLines = lines[0].Replace("**** This was broadcast by ", "").Replace(" EVE ****", "").Split(new[] { " at " }, StringSplitOptions.RemoveEmptyEntries);
                var recipient = lines[lines.Length - 1].Replace("**** Message sent to the ", "").Replace(" Group ****", "");

                var sendLines = new List<string>();
                sendLines.Add(string.Format("Timestamp: {0}", senderLines[1]));
                sendLines.Add(string.Format("From: {0}", senderLines[0]));
                sendLines.Add(string.Format("To: {0}", recipient));
                //sendLines.Add(lines[1]);

                for (var i = 1; i < lines.Length - 1; i++)
                {
                    sendLines.Add(lines[i]);
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("{0}>> {1}", msg.From.User, msg.Body);
                Console.ForegroundColor = ConsoleColor.Green;

                var payload = new MessagePayload();
                payload.Attachments = new List<MessagePayloadAttachment>();

                if(!sendLines[0].Contains("@everyone"))
                {
                    //sendLines.Insert(0, "@everyone");
                    sendLines[0] = string.Concat("@everyone: ", sendLines[0]);
                }
                payload.Attachments.Add(new MessagePayloadAttachment()
                {
                    Text = string.Join("\n", sendLines).Replace("@everyone", "@channel"),
                    Title = string.Format("{0}: Message from {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "Marvin"),
                    Colour = "#ff0066"
                });
                Plugin.SendToRoom(payload, Properties.Settings.Default.SlackRoom, Properties.Settings.Default.SlackCorpWebhook, Properties.Settings.Default.BotName);

                if (msg.Body.Contains(Properties.Settings.Default.XMPPGroupName))
                {
                    foreach (var wHook in Properties.Settings.Default.SlackSharedWebhooks)
                    {
                        Plugin.SendToRoom(payload, Properties.Settings.Default.SlackRoom, wHook, Properties.Settings.Default.BotName);
                    }
                }
            }
        }

        private void InitializeComponent()
        {
            // 
            // JabberBot
            // 
            this.ServiceName = "JabberBot";

        }
    }

}
