using System;
using JetBrains.Annotations;

namespace Libota.Desktop.Infrastructure.Attributes;
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[MeansImplicitUse]
public class SingleInstanceViewAttribute : Attribute;