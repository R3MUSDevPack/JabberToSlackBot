using agsXMPP;
using agsXMPP.Collections;
using agsXMPP.protocol.client;
using agsXMPP.Xml.Dom;
using R3MUS.Devpack.Slack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace R3MUS.Devpack.Jabber
{
    public class JabberWotchy : ServiceBase
    {
        private bool _wait;
        private XmppClientConnection xmpp;

        static void Main(string[] args)
        {
            if(Environment.UserInteractive)
            {
                var jabberWotchy = new JabberWotchy(args);
                jabberWotchy.Start();
            }
            else
            {
                ServiceBase.Run(new JabberWotchy(args));
            }
        }
        
        public JabberWotchy(string[] args)
        {

        }

        protected override void OnStart(string[] args)
        {
            Start();
        }

        protected override void OnStop()
        {
            Stop();
        }

        public void Start()
        {
            if (Environment.UserInteractive)
            {
                try
                {
                    Console.Title = "Jabber Client";
                    Console.ForegroundColor = ConsoleColor.White;

                    var payload = new MessagePayload();
                    payload.Attachments = new List<MessagePayloadAttachment>();

                    payload.Attachments.Add(new MessagePayloadAttachment()
                    {
                        Text = "I just want you to know, I'm feeling very, very depressed.",
                        Title = "Marvin is waking up...",
                        Colour = "#ff0066"
                    });
                    Plugin.SendToRoom(payload, "it", Properties.Settings.Default.SlackWebhook, Properties.Settings.Default.BroadcastName);
                }
                catch (Exception ex)
                {
                    var payload = new MessagePayload();
                    payload.Attachments = new List<MessagePayloadAttachment>();

                    payload.Attachments.Add(new MessagePayloadAttachment()
                    {
                        Text = "Life? Don't talk to me about life.",
                        Title = "Marvin won't wake up. Try oiling his nuts.",
                        Colour = "#ff0066"
                    });
                    payload.Attachments.Add(new MessagePayloadAttachment()
                    {
                        Text = ex.Message,
                        Title = "Marvin system boot error",
                        Colour = "#ff0066"
                    });
                    Plugin.SendToRoom(payload, "it_testing", Properties.Settings.Default.SlackWebhook, Properties.Settings.Default.BroadcastName);
                }
            }
            try
            {
                ConsoleWriteLine("Login");
                ConsoleWriteLine("");
                
                ConsoleWriteLine(string.Format("JID: {0}", Properties.Settings.Default.UserName));

                Jid jidSender = new Jid(Properties.Settings.Default.UserName);
                xmpp = new XmppClientConnection(jidSender.Server);
                xmpp.Open(jidSender.User, Properties.Settings.Default.Password);

                xmpp.OnLogin += new ObjectHandler(xmpp_OnLogin);
                xmpp.OnSocketError += new ErrorHandler(xmpp_OnSocketError);
                xmpp.OnError += new ErrorHandler(xmpp_OnSocketError);
                xmpp.OnStreamError += new XmppElementHandler(xmpp_OnStreamError);

                ConsoleWrite("Wait for Login ");

                var i = 0;
                _wait = true;

                do
                {
                    ConsoleWrite(".");
                    Thread.Sleep(500);
                }
                while (_wait);
            }
            catch(Exception ex)
            {
                var payload = new MessagePayload();
                payload.Attachments = new List<MessagePayloadAttachment>();

                payload.Attachments.Add(new MessagePayloadAttachment()
                {
                    Text = "Life? Don't talk to me about life.",
                    Title = "Marvin won't wake up. Try oiling his nuts.",
                    Colour = "#ff0066"
                });
                payload.Attachments.Add(new MessagePayloadAttachment()
                {
                    Text = ex.Message,
                    Title = "Marvin system boot error",
                    Colour = "#ff0066"
                });
                Plugin.SendToRoom(payload, "it_testing", Properties.Settings.Default.SlackWebhook, Properties.Settings.Default.BroadcastName);
            }
        }

        private void xmpp_OnSocketError(object sender, Exception ex)
        {
            Restart();
        }
        private void xmpp_OnStreamError(object sender, Element e)
        {
            Restart();
        }

        private void Restart()
        {
            var payload = new MessagePayload();
            payload.Attachments = new List<MessagePayloadAttachment>();

            payload.Attachments.Add(new MessagePayloadAttachment()
            {
                Text = "Life? Don't talk to me about life.",
                Title = "Marvin won't talk to anyone. Try oiling his nuts.",
                Colour = "#ff0066"
            });
            Plugin.SendToRoom(payload, "it_testing", Properties.Settings.Default.SlackWebhook, Properties.Settings.Default.BroadcastName);
            Stop();
            Start();
        }

        private void xmpp_OnLogin(object sender)
        {
            OnLogin();
        }

        private void OnLogin()
        {
            try
            {
                _wait = false;

                ConsoleWriteLine("Login Status:");
                ConsoleWriteLine(string.Format("xmpp Connection State {0}", xmpp.XmppConnectionState));
                ConsoleWriteLine(string.Format("xmpp Authenticated? {0}", xmpp.Authenticated));
                ConsoleWriteLine("");
                
                ConsoleWriteLine("Sending Presence");
                Presence p = new Presence(ShowType.chat, "Online");
                p.Type = PresenceType.available;
                xmpp.Send(p);
                ConsoleWriteLine("");
                
                ConsoleWriteLine(string.Format("Listening to {0}", Properties.Settings.Default.ListenTo));

                xmpp.MessageGrabber.Add(new Jid(Properties.Settings.Default.ListenTo),
                                         new BareJidComparer(),
                                         new MessageCB(MessageCallBack),
                                         null);

                ConsoleWriteLine("", ConsoleColor.White);
            }
            catch(Exception ex)
            {
                var payload = new MessagePayload();
                payload.Attachments = new List<MessagePayloadAttachment>();
                payload.Attachments.Add(new MessagePayloadAttachment()
                {
                    Text = ex.Message,
                    Title = "Marvin system boot error",
                    Colour = "#ff0066"
                });
                Plugin.SendToRoom(payload, "it_testing", Properties.Settings.Default.SlackWebhook, Properties.Settings.Default.BroadcastName);
                Stop();
                Start();
            }
        }

        static void MessageCallBack(object sender, agsXMPP.protocol.client.Message msg, object data)
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

                for (var i = 1; i < lines.Length - 1; i++)
                {
                    sendLines.Add(lines[i]);
                }
                
                ConsoleWriteLine(string.Format("{0}>> {1}", msg.From.User, msg.Body), ConsoleColor.Red);

                var payload = new MessagePayload();
                //payload.Text = string.Format("@channel: From {0}", senderLines[0]);
                payload.Attachments = new List<MessagePayloadAttachment>();
                if (!msg.Body.Contains("@everyone"))
                {
                    sendLines.Insert(0, "@everyone");
                }
                payload.Attachments.Add(new MessagePayloadAttachment()
                {
                    Text = new Censor().CensorText(string.Join("\n", sendLines)),
                    Title = string.Format("{0}: Message from {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Properties.Settings.Default.BroadcastShortName),
                    Colour = "#ff0066"
                });
                Plugin.SendToRoom(payload, Properties.Settings.Default.Room, Properties.Settings.Default.SlackWebhook, Properties.Settings.Default.BroadcastName);

                if (msg.Body.Contains("bog_all"))
                {
                    foreach (var webhook in Properties.Settings.Default.SharedWebhooks)
                    {
                        Plugin.SendToRoom(payload, Properties.Settings.Default.Room, webhook, Properties.Settings.Default.BroadcastName);
                    }
                }
            }
        }

        static void ConsoleWriteLine(string output, System.ConsoleColor color = ConsoleColor.White, bool consoleReadLine = false)
        {
            if(Environment.UserInteractive)
            {
                try
                {
                    Console.ForegroundColor = color;
                    Console.WriteLine(output);
                    Console.ForegroundColor = ConsoleColor.White;
                    if (consoleReadLine)
                    {
                        Console.ReadLine();
                    }
                }
                catch(Exception ex)
                {
                }
            }
        }

        static void ConsoleWrite(string output, System.ConsoleColor color = ConsoleColor.White, bool consoleReadLine = false)
        {
            if (Environment.UserInteractive)
            {
                try
                {
                    Console.ForegroundColor = color;
                    Console.Write(output);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.ReadLine();
                }
                catch (Exception ex)
                { }
            }
        }

        private void Stop()
        {
            var payload = new MessagePayload();
            payload.Attachments = new List<MessagePayloadAttachment>();

            payload.Attachments.Add(new MessagePayloadAttachment()
            {
                Text = "Life? Don't talk to me about life.",
                Title = "Marvin is shutting down. You might want to find up what's up with the grouchy bastard.",
                Colour = "#ff0066"
            });
            Plugin.SendToRoom(payload, "it", Properties.Settings.Default.SlackWebhook, Properties.Settings.Default.BroadcastName);

            xmpp.Close();
            xmpp = null;
        }        
    }
}
