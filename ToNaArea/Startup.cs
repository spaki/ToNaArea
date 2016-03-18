using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ToNaArea.Startup))]
namespace ToNaArea
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
