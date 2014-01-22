using System;
using Owin;
using Elmah;

static class Startup
{
    public static void Configuration(IAppBuilder app)
    {
        if (app == null) throw new ArgumentNullException("app");
        app.UseElmahWeb();
    }
}