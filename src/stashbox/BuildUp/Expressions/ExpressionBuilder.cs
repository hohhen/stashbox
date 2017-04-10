﻿using Stashbox.Entity;
using Stashbox.Infrastructure;
using Stashbox.Infrastructure.ContainerExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Stashbox.Configuration;
using Stashbox.Entity.Resolution;
using Stashbox.Infrastructure.Registration;

namespace Stashbox.BuildUp.Expressions
{
    internal class ExpressionBuilder : IExpressionBuilder
    {
        private readonly IContainerContext containerContext;
        private readonly IContainerExtensionManager containerExtensionManager;

        public ExpressionBuilder(IContainerContext containerContext, IContainerExtensionManager containerExtensionManager)
        {
            this.containerContext = containerContext;
            this.containerExtensionManager = containerExtensionManager;
        }

        public Expression CreateFillExpression(IServiceRegistration serviceRegistration, Expression instance,
            ResolutionInfo resolutionInfo, Type serviceType)
        {
            var block = new List<Expression>();

            if (instance.Type != serviceType)
                instance = Expression.Convert(instance, serviceType);

            var variable = Expression.Variable(serviceType);
            var assign = Expression.Assign(variable, instance);

            block.Add(assign);

            if (serviceRegistration.MetaInformation.InjectionMembers.Length > 0)
                block.AddRange(this.FillMembersExpression(serviceRegistration, resolutionInfo, variable));
            
            if (serviceRegistration.MetaInformation.InjectionMethods.Length > 0 || this.containerExtensionManager.HasPostBuildExtensions)
                return this.CreatePostWorkExpressionIfAny(serviceRegistration, resolutionInfo, variable, serviceType, block, variable);

            block.Add(variable); //return

            return Expression.Block(new[] { variable }, block);
        }

        public Expression CreateExpression(IServiceRegistration serviceRegistration, ResolutionInfo resolutionInfo, Type serviceType)
        {
            var rule = serviceRegistration.RegistrationContext.ConstructorSelectionRule ?? this.containerContext.ContainerConfigurator.ContainerConfiguration.ConstructorSelectionRule;
            var constructors = rule(serviceRegistration.MetaInformation.Constructors).ToArray();

            var constructor = this.SelectConstructor(serviceRegistration, resolutionInfo, constructors);
            if (constructor == null) return null;
                
            Expression initExpression = Expression.New(constructor.Constructor, constructor.Parameters);

            if (serviceRegistration.MetaInformation.InjectionMembers.Length > 0)
                initExpression = Expression.MemberInit((NewExpression)initExpression, this.GetMemberBindings(serviceRegistration, resolutionInfo));
           
            if (serviceRegistration.MetaInformation.InjectionMethods.Length > 0 || this.containerExtensionManager.HasPostBuildExtensions)
                return this.CreatePostWorkExpressionIfAny(serviceRegistration, resolutionInfo, initExpression, serviceType);

            return initExpression;
        }

        private ResolutionConstructor SelectConstructor(IServiceRegistration serviceRegistration, ResolutionInfo resolutionInfo, ConstructorInfo[] constructors)
        {
            var length = constructors.Length;

            for (var i = 0; i < length; i++)
            {
                var constructor = constructors[i];
                var parameters = constructor.GetParameters();
                var paramLength = parameters.Length;
                var parameterExpressions = new Expression[paramLength];

                var hasNullParameter = false;
                for (var j = 0; j < paramLength; j++)
                {
                    var parameter = parameters[j];

                    var expression = this.containerContext.ResolutionStrategy.BuildResolutionExpression(this.containerContext, resolutionInfo, 
                    serviceRegistration.MetaInformation.GetTypeInformationForParameter(parameter), 
                    serviceRegistration.RegistrationContext.InjectionParameters);

                    if(expression == null)
                    {
                        hasNullParameter = true;
                        break;
                    }

                    parameterExpressions[j] = expression;
                }

                if(hasNullParameter) continue;

                return new ResolutionConstructor { Constructor = constructor, Parameters = parameterExpressions};
            }

            return null;
        }

        private Expression CreatePostWorkExpressionIfAny(IServiceRegistration serviceRegistration, ResolutionInfo resolutionInfo,
            Expression initExpression, Type serviceType, List<Expression> block = null, ParameterExpression variable = null)
        {
            block = block ?? new List<Expression>();

            var newVariable = variable ?? Expression.Variable(initExpression.Type);
            if (variable == null)
            {
                var assign = Expression.Assign(newVariable, initExpression);
                block.Add(assign);
            }

            if (serviceRegistration.MetaInformation.InjectionMethods.Length > 0)
                block.AddRange(this.CreateMethodExpressions(serviceRegistration, resolutionInfo, newVariable));

            if (this.containerExtensionManager.HasPostBuildExtensions)
            {
                var call = Expression.Call(Expression.Constant(this.containerExtensionManager), Constants.BuildExtensionMethod, newVariable, Expression.Constant(this.containerContext),
                      Expression.Constant(resolutionInfo), Expression.Constant(serviceType), Expression.Constant(serviceRegistration.RegistrationContext.InjectionParameters, typeof(InjectionParameter[])));

                block.Add(Expression.Assign(newVariable, Expression.Convert(call, serviceType)));
            }

            block.Add(newVariable); //return

            return Expression.Block(new[] { newVariable }, block);
        }

        private IEnumerable<Expression> FillMembersExpression(IServiceRegistration serviceRegistration, ResolutionInfo resolutionInfo, Expression instance)
        {
            var length = serviceRegistration.MetaInformation.InjectionMembers.Length;

            var expressions = new List<Expression>();

            for (var i = 0; i < length; i++)
            {
                var member = serviceRegistration.MetaInformation.InjectionMembers[i];

                if (!this.CanInjectMember(member, serviceRegistration)) continue;

                var expression = this.containerContext.ResolutionStrategy
                    .BuildResolutionExpression(this.containerContext, resolutionInfo, member.TypeInformation, serviceRegistration.RegistrationContext.InjectionParameters);

                if (expression == null) continue;

                if (member.MemberInfo is PropertyInfo prop)
                {
                    var propExpression = Expression.Property(instance, prop);
                    expressions.Add(Expression.Assign(propExpression, expression));
                }
                else if (member.MemberInfo is FieldInfo field)
                {
                    var propExpression = Expression.Field(instance, field);
                    expressions.Add(Expression.Assign(propExpression, expression));
                }
            }

            return expressions;
        }

        private Expression[] CreateMethodExpressions(IServiceRegistration serviceRegistration, ResolutionInfo resolutionInfo, Expression newExpression)
        {
            var length = serviceRegistration.MetaInformation.InjectionMethods.Length;
            var methodExpressions = new Expression[length];

            for (var i = 0; i < length; i++)
            {
                var info = serviceRegistration.MetaInformation.InjectionMethods[i];

                var paramLength = info.Parameters.Length;
                if (paramLength == 0)
                    methodExpressions[i] = Expression.Call(newExpression, info.Method, new Expression[0]);
                else
                {
                    var parameters = new Expression[paramLength];
                    for (var j = 0; j < paramLength; j++)
                        parameters[j] = this.containerContext.ResolutionStrategy.BuildResolutionExpression(this.containerContext, resolutionInfo,
                            info.Parameters[j], serviceRegistration.RegistrationContext.InjectionParameters);

                    methodExpressions[i] = Expression.Call(newExpression, info.Method, parameters);
                }
            }

            return methodExpressions;
        }

        private IEnumerable<MemberBinding> GetMemberBindings(IServiceRegistration serviceRegistration, ResolutionInfo resolutionInfo)
        {
            var length = serviceRegistration.MetaInformation.InjectionMembers.Length;
            var members = new List<MemberBinding>();

            for (var i = 0; i < length; i++)
            {
                var info = serviceRegistration.MetaInformation.InjectionMembers[i];
                if (!this.CanInjectMember(info, serviceRegistration)) continue;

                var expression = this.containerContext.ResolutionStrategy
                    .BuildResolutionExpression(this.containerContext, resolutionInfo, info.TypeInformation, serviceRegistration.RegistrationContext.InjectionParameters);

                if (expression == null) continue;

                members.Add(Expression.Bind(info.MemberInfo, expression));
            }

            return members;
        }

        private bool CanInjectMember(MemberInformation member, IServiceRegistration serviceRegistration)
        {
            var autoMemberInjectionEnabled = this.containerContext.ContainerConfigurator.ContainerConfiguration.MemberInjectionWithoutAnnotationEnabled || serviceRegistration.RegistrationContext.AutoMemberInjectionEnabled;
            var autoMemberInjectionRule = serviceRegistration.RegistrationContext.AutoMemberInjectionEnabled ? serviceRegistration.RegistrationContext.AutoMemberInjectionRule :
                this.containerContext.ContainerConfigurator.ContainerConfiguration.MemberInjectionWithoutAnnotationRule;

            if (autoMemberInjectionEnabled)
                return member.TypeInformation.HasDependencyAttribute ||
                    member.MemberInfo is FieldInfo && autoMemberInjectionRule.HasFlag(Rules.AutoMemberInjection.PrivateFields) ||
                    member.MemberInfo is PropertyInfo && (autoMemberInjectionRule.HasFlag(Rules.AutoMemberInjection.PropertiesWithPublicSetter) && ((PropertyInfo)member.MemberInfo).HasSetMethod() ||
                     autoMemberInjectionRule.HasFlag(Rules.AutoMemberInjection.PropertiesWithLimitedAccess));

            return member.TypeInformation.HasDependencyAttribute;
        }
    }
}
