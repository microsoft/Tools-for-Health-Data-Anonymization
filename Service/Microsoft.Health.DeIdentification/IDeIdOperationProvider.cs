﻿namespace Microsoft.Health.DeIdentification.Contract
{
    public interface IDeIdOperationProvider
    {
        public IDeIdOperation<TSource, TResult> CreateDeIdOperation<TSource, TResult>(DeIdConfiguration deIdRuleSet);
    }
}
