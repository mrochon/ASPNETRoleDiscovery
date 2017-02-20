using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Authorize2AppRole
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var p = new Program();
            var fo = new OpenFileDialog()
            {
                AddExtension = true,
                CheckFileExists = true,
                DefaultExt = "dll",
                Filter = "Assemblies (*.dll)|*.dll",
                Title = "Assembly to analyze",
                InitialDirectory = @"C:\temp\WebApplication2\WebApplication2\bin"
            };
            if (fo.ShowDialog() == DialogResult.OK)
            {
                var fileName = fo.FileName; 
                var ass = Assembly.ReflectionOnlyLoadFrom(fileName);
                var roles = p.GetAllRoles(fileName);
                Console.WriteLine("ClassName, MethodName, RoleName");
                foreach (var r in roles)
                {
                    if (r.Member is Type)
                        Console.WriteLine("{0},*,{1}", ((Type)r.Member).FullName, r.Role);
                    else
                        Console.WriteLine("{0},{2},{1}", r.Member.DeclaringType.FullName, r.Role, r.Member.Name);
                }
                Console.WriteLine("---------------------------------");

                Console.WriteLine(@"""appRoles"": [");
                foreach (var role in roles.Select(r => r.Role).Distinct())
                {
                    var roleDef = new
                    {
                        allowedMemberTypes = new string[] { "Application", "User" },
                        description = "<some description>",
                        displayName = role,
                        id = Guid.NewGuid().ToString(),
                        isEnabled = true,
                        value = role
                    };
                    Console.WriteLine(JsonConvert.SerializeObject(roleDef, new JsonSerializerSettings() { Formatting = Formatting.Indented }));
                }
                Console.WriteLine("],");
            }

            Console.ReadKey();
        }

        Type _controller;
        Type _authAttr;
        public IEnumerable<RoleDef> GetAllRoles(string assemblyName)
        {
            var webAppAssembly = Path.GetFileName(assemblyName);
            var dir = Path.GetDirectoryName(assemblyName) + "/";
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += (obj, args) =>
            {
                var nameParts = args.Name.Split(',');
                var assName = dir + nameParts[0] + ".dll";
                try
                {
                    if (File.Exists(assName))
                        return Assembly.ReflectionOnlyLoadFrom(assName);
                    return Assembly.ReflectionOnlyLoad(args.Name);
                } catch(Exception ex)
                {
                    throw;
                }
            };
            var x = File.Exists(dir + "System.Web.Mvc" + ".dll");
            var mvcAss = Assembly.ReflectionOnlyLoadFrom(dir + "System.Web.Mvc" + ".dll");
            _controller = mvcAss.GetType("System.Web.Mvc.Controller");
            _authAttr = mvcAss.GetType("System.Web.Mvc.AuthorizeAttribute");
            var ass = Assembly.ReflectionOnlyLoadFrom(dir + webAppAssembly);
            IEnumerable<RoleDef> roles = new List<RoleDef>();
            foreach(var type in ass.GetExportedTypes())
            {
                if (!type.IsSubclassOf(_controller))
                    continue;
                // Class roles
                var newRoles = GetMemberRoles(type);
                if (newRoles != null)
                    roles = roles.Concat(newRoles.Select(r => new RoleDef { Member = type, Role = r })).Distinct();
                // Method roles
                foreach(var m in type.GetMethods())
                {
                    newRoles = GetMemberRoles(m);
                    if (newRoles != null)
                        roles = roles.Concat(newRoles.Select(r => new RoleDef { Member = m, Role = r })).Distinct();
                }
            }
            return roles;
        }
        public IEnumerable<string> GetMemberRoles(MemberInfo obj)
        {
            IEnumerable<string> roles = new List<string>();
            roles = obj.GetCustomAttributesData().
                Where(attr => (attr.AttributeType == _authAttr) || (attr.AttributeType.IsSubclassOf(_authAttr))).
                SelectMany(attr => attr.NamedArguments).
                Where(arg => arg.MemberName == "Roles").
                SelectMany(arg =>
                {
                    var rolesParam = (string)arg.TypedValue.Value;
                    if (!String.IsNullOrEmpty(rolesParam))
                        return (IEnumerable<string>)rolesParam.Split(',');
                    else
                        return new List<string>();
                });
            return roles;
        }
    }
    public class RoleDef
    {
        public MemberInfo Member { get; set; }
        public string Role { get; set; }
    }
}
