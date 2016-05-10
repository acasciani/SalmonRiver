using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SalmonRiver.Startup))]
namespace SalmonRiver
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
