namespace MoonlightServers.Daemon.Helpers;

public class CompositeServiceProvider : IServiceProvider
{
    private readonly List<IServiceProvider> ServiceProviders;

    public CompositeServiceProvider(params IServiceProvider[] serviceProviders)
    {
        ServiceProviders = new List<IServiceProvider>(serviceProviders);
    }

    public object? GetService(Type serviceType)
    {
        foreach (var provider in ServiceProviders)
        {
            try
            {
                var service = provider.GetService(serviceType);

                if (service != null)
                    return service;
            }
            catch (InvalidOperationException)
            {
                // Ignored
            }
        }

        return null;
    }
}