#if MAUI_DEVFLOW
using Microsoft.Maui.DevFlow.Agent;
#endif

namespace Wordzzle;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder()
			.UseMauiApp<App>();

#if MAUI_DEVFLOW
        builder.AddMauiDevFlowAgent();
#endif

		return builder.Build();
	}
}
