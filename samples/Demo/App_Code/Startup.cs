using Owin;
using Elmah;

static class Startup
{
	public static void Configuration(IAppBuilder app)
	{
        app.UseElmahWeb();
	}
}