using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ignite.SharpNetSH;
using NetFwTypeLib;
using Popcorn.Utils;

namespace Popcorn.Handler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Contains("acl"))
                RegisterUrlAcl();
            if (args.Contains("fw"))
                RegisterFirewallRule();
        }

        private static void RegisterUrlAcl()
        {
            var username = Environment.GetEnvironmentVariable("USERNAME");
            var domain = Environment.GetEnvironmentVariable("USERDOMAIN");
            var netsh = new NetSH(new Utils.CommandLineHarness());
            var addResponse = netsh.Http.Add.UrlAcl(Constants.ServerUrl, $"{domain}\\{username}", true);
        }

        private static void RegisterFirewallRule()
        {
            INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FWRule"));
            firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            firewallRule.Description = "Enables Popcorn server.";
            firewallRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
            firewallRule.Enabled = true;
            firewallRule.InterfaceTypes = "All";
            firewallRule.Name = "Popcorn Server";
            firewallRule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
            firewallRule.LocalPorts = "9900";
            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
            firewallPolicy.Rules.Add(firewallRule);
        }
    }
}
