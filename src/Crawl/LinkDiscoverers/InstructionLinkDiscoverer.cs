using AngleSharp.Html.Dom;
using Crawl.Core.Interfaces;
using Crawl.Models;

namespace Crawl.LinkDiscoverers;

public class InstructionLinkDiscoverer : ILinkDiscoverer
{
    private Func<ICollection<DiscoveredLink>> _discoverFunction = () => [];
    private Func<ICollection<DiscoveredLink>> _accumulationFunction = () => [];
    
    
    
    public InstructionLinkDiscoverer() { }
    
    /*
    public ICollection<DiscoveredLink> DiscoverLinks(FetchResult context, IHtmlDocument? document, CancellationToken cancellationToken = default)
    {
        (ICollection<DiscoveredLink> links, bool transitionCondition) = Current.Invoke(document);

        if (transitionCondition)
        {
            TransitionState();
        }

        return links;
    }

    public InstructionLinkDiscoverer Do(Func<IHtmlDocument?, (ICollection<DiscoveredLink>, bool)> function)
    {
        _stateFunctions.Enqueue(function);

        return this;
    }

    public InstructionLinkDiscoverer Then(Func<IHtmlDocument?, (ICollection<DiscoveredLink>, bool)> function)
    {
        Do(function);

        return this;
    }

    private void TransitionState()
    {
        _stateFunctions.Dequeue();
    }*/
    public ICollection<DiscoveredLink> DiscoverLinks(FetchResult context, IHtmlDocument? document, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}