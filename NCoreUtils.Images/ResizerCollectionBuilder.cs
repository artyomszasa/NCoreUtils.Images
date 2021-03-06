using System.Collections.Generic;
using NCoreUtils.Images.Internal;

namespace NCoreUtils.Images
{
    public class ResizerCollectionBuilder
    {
        readonly Dictionary<string, IResizerFactory> _factories = new Dictionary<string, IResizerFactory>();

        public ResizerCollectionBuilder Add(string name, IResizerFactory factory)
        {
            _factories.Add(name, factory);
            return this;
        }

        public ResizerCollection Build()
            => new ResizerCollection(_factories);
    }
}