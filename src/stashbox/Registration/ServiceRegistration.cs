﻿using Stashbox.Entity;
using Stashbox.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Stashbox.Registration
{
    public class ServiceRegistration : IServiceRegistration
    {
        private readonly ILifetime lifetimeManager;
        private readonly IObjectBuilder objectBuilder;
        private readonly IContainerContext containerContext;
        private readonly HashSet<Type> attributeConditions;
        private readonly Type targetTypeCondition;
        private readonly Func<TypeInformation, bool> resolutionCondition;

        public ServiceRegistration(ILifetime lifetimeManager, IObjectBuilder objectBuilder, IContainerContext containerContext, HashSet<Type> attributeConditions = null,
            Type targetTypeCondition = null, Func<TypeInformation, bool> resolutionCondition = null)
        {
            this.lifetimeManager = lifetimeManager;
            this.objectBuilder = objectBuilder;
            this.containerContext = containerContext;
            this.attributeConditions = attributeConditions;
            this.targetTypeCondition = targetTypeCondition;
            this.resolutionCondition = resolutionCondition;
        }

        public object GetInstance(ResolutionInfo resolutionInfo)
        {
            return this.lifetimeManager.GetInstance(this.objectBuilder, this.containerContext, resolutionInfo);
        }

        public bool IsUsableForCurrentContext(TypeInformation typeInfo)
        {
            return (this.targetTypeCondition == null && this.resolutionCondition == null && (this.attributeConditions == null || !this.attributeConditions.Any())) ||
                   (this.targetTypeCondition != null && typeInfo.ParentType != null && this.targetTypeCondition == typeInfo.ParentType) ||
                   (this.attributeConditions != null && typeInfo.CustomAttributes != null &&
                    this.attributeConditions.Intersect(typeInfo.CustomAttributes.Select(attribute => attribute.GetType())).Any()) ||
                   (this.resolutionCondition != null && this.resolutionCondition(typeInfo));
        }

        public bool HasCondition => this.targetTypeCondition != null || this.resolutionCondition != null ||
            (this.attributeConditions != null && this.attributeConditions.Any());

        public void CleanUp()
        {
            this.objectBuilder.CleanUp();
            this.lifetimeManager.CleanUp();
        }
    }
}
