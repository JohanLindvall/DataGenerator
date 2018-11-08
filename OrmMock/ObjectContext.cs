﻿// Copyright(c) 2017, 2018 Johan Lindvall
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace OrmMock
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    using Fasterflect;

    /// <summary>
    /// Defines an object context containing the created object.
    /// </summary>
    public class ObjectContext
    {
        /// <summary>
        /// Holds the number of objects generated.
        /// </summary>
        private int objectCount;

        /// <summary>
        /// Holds the list of created objects.
        /// </summary>
        private readonly List<object> createdObjects = new List<object>();

        /// <summary>
        /// Holds the created singletons.
        /// </summary>
        private readonly Dictionary<Type, object> singletons = new Dictionary<Type, object>();

        /// <summary>
        /// Holds the method property cache.
        /// </summary>
        private readonly Dictionary<Type, List<Action<object, IList<object>, bool>>> methodPropertyCache = new Dictionary<Type, List<Action<object, IList<object>, bool>>>();

        /// <summary>
        /// Holds the constructor cache for the given type.
        /// </summary>
        private readonly Dictionary<Type, Func<IList<object>, object>> constructorCache = new Dictionary<Type, Func<IList<object>, object>>();

        /// <summary>
        /// Holds the object logging chain. Only used if logging is enabled.
        /// </summary>
        private readonly IList<IList<object>> loggingChain = new List<IList<object>>();

        /// <summary>
        /// Holds the structure data.
        /// </summary>
        private readonly Structure structure;

        /// <summary>
        /// Holds the random generator used by this instance.
        /// </summary>
        private readonly Random random = new Random();

        /// <summary>
        /// Gets or sets the limit of how many object to create in one pass.
        /// </summary>
        public int ObjectLimit { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the limit of how deep the object hierarchy can be.
        /// </summary>
        public int RecursionLimit { get; set; } = 100;

        /// <summary>
        /// Gets or sets the number of items to create in root level object collections.
        /// </summary>
        public int RootCollectionMembers { get; set; } = 3;

        /// <summary>
        /// Gets or sets the number of items to create in leaf level object collections.
        /// </summary>
        public int NonRootCollectionMembers { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value determining if logging of object creation should be enabled.
        /// </summary>
        public bool Logging { get; set; }

        public ObjectContext(Structure structure)
        {
            this.structure = structure;
        }

        /// <summary>
        /// Resets the internally stored created objects.
        /// </summary>
        public void Reset()
        {
            this.createdObjects.Clear();
            this.objectCount = 0;
        }

        /// <summary>
        /// Registers a specific object type to be a singleton of a specific value
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="value">The singleton value to register.</param>
        /// <returns>The generator.</returns>
        public ObjectContext Singleton<T>(T value)
        {
            this.singletons.Add(typeof(T), value);

            return this;
        }

        /// <summary>
        /// Gets a singleton value for the given type.
        /// </summary>
        /// <typeparam name="T">The singleton type.</typeparam>
        /// <returns>The singleton value.</returns>
        public T GetSingleton<T>()
        {
            return (T)this.singletons[typeof(T)];
        }

        /// <summary>
        /// Gets the object of the specified type..
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <returns>The object at the given index.</returns>
        public T GetObject<T>()
        {
            return (T)this.createdObjects.Single(o => o.GetType() == typeof(T));
        }

        /// <summary>
        /// Gets the object of the specified type at the given index.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="index">The index of the object to get.</param>
        /// <returns>The object at the given index.</returns>
        public T GetObject<T>(int index)
        {
            return this.GetObjects<T>().Skip(index).First();
        }

        /// <summary>
        /// Gets the object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <returns>The objects.</returns>
        public IEnumerable<T> GetObjects<T>()
        {
            return this.createdObjects.Where(o => o.GetType() == typeof(T)).Select(o => (T)o);
        }

        /// <summary>
        /// Gets the object of the specified type at the given index.
        /// </summary>
        /// <param name="index">The index of the object to get.</param>
        /// <returns>The object at the given index.</returns>
        public object GetObject(int index)
        {
            return this.createdObjects.Skip(index).First();
        }

        /// <summary>
        /// Gets the object of the specified type.
        /// </summary>
        /// <returns>The objects.</returns>
        public IEnumerable<object> GetObjects()
        {
            return this.createdObjects;
        }

        /// <summary>
        /// Creates an object of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <returns>The created object.</returns>
        public T Create<T>()
        {
            return (T)CreateObject(typeof(T), new List<object>());
        }

        /// <summary>
        /// Creates an object of the given type.
        /// </summary>
        /// <param name="t">The type of the object.</param>
        /// <returns>The created object.</returns>
        public object Create(Type t)
        {
            return CreateObject(t, new List<object>());
        }

        /// <summary>
        /// Creates an object of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <typeparam name="T2">The type of the parameter.</typeparam>
        /// <param name="e">The setter expression</param>
        /// <param name="value">The value</param>
        /// <returns>The created object.</returns>
        public T Create<T, T2>(Expression<Func<T, T2>> e, T2 value)
        {
            var result = this.Create<T>();

            foreach (var property in ExpressionUtility.GetPropertyInfo(e))
            {
                property.SetMethod.Invoke(result, new[] { (object)value });
            }

            return result;
        }

        /// <summary>
        /// Creates many objects of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <returns>The created objects.</returns>
        public IEnumerable<T> CreateMany<T>(int create = 3)
        {
            while (create-- > 0)
            {
                yield return this.Create<T>();
            }
        }

        /// <summary>
        /// Creates many objects of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <typeparam name="T2">The type of the parameter.</typeparam>
        /// <param name="e">The setter expression</param>
        /// <param name="values">The values</param>
        /// <returns>The created objects.</returns>
        public IEnumerable<T> CreateMany<T, T2>(Expression<Func<T, T2>> e, params T2[] values)
        {
            foreach (var value in values)
            {
                yield return this.Create(e, value);
            }
        }

        /// <summary>
        /// Creates a string of the given length.
        /// </summary>
        /// <param name="length">The string length.</param>
        /// <returns>A random string.</returns>
        public string CreateString(int length)
        {
            return this.CreateString(string.Empty, length);
        }

        /// <summary>
        /// Creates a random string with the given prefix and maximum length.
        /// </summary>
        /// <param name="prefix">The prefix</param>
        /// <param name="length">The maximum length</param>
        /// <returns>A random string.</returns>
        public string CreateString(string prefix, int length)
        {
            var ba = Math.Max(0, length - prefix.Length);
            ba *= 3;
            if (ba % 4 != 0)
            {
                ba += 4;
            }
            ba /= 4;

            var rnd = new byte[ba];
            this.random.NextBytes(rnd);
            var result = prefix + Convert.ToBase64String(rnd);
            if (result.Length > length)
            {
                result = result.Substring(0, length);
            }

            return result;
        }

        /// <summary>
        /// Creates an object of type t, considering the sources for references.
        /// </summary>
        /// <param name="objectType">The type of the object to create.</param>
        /// <param name="sourceObjects">The sources.</param>
        /// <returns></returns>
        private object CreateObject(Type objectType, IList<object> sourceObjects)
        {
            if (this.Logging && sourceObjects.Count == 0)
            {
                this.loggingChain.Clear();
            }

            if (!this.constructorCache.TryGetValue(objectType, out var constructor))
            {
                var simpleCreator = this.ValueCreator(objectType);

                if (simpleCreator != null)
                {
                    constructor = _ => simpleCreator(string.Empty);
                }
                else
                {
                    var ctor = objectType.GetConstructors().SingleOrDefault();

                    if (ctor == null)
                    {
                        throw new InvalidOperationException($@"Cannot construct {objectType.Name}.");
                    }

                    var ctorParameters = new List<Func<IList<object>, object>>();

                    foreach (var constructorParameter in ctor.GetParameters())
                    {
                        var constructorParameterType = constructorParameter.ParameterType;

                        if (!this.structure.ConstructorCustomization.TryGetValue(constructorParameterType, out var effectiveCreationOptions))
                        {
                            if (!this.structure.TypeCustomization.TryGetValue(constructorParameterType, out effectiveCreationOptions))
                            {
                                effectiveCreationOptions = CreationOptions.Default;
                            }
                        }

                        if (effectiveCreationOptions == CreationOptions.Skip)
                        {
                            ctorParameters.Add(_ => null);
                            continue;
                        }

                        var valueCreator = this.ValueCreator(constructorParameterType);

                        if (valueCreator != null)
                        {
                            ctorParameters.Add(_ => valueCreator(objectType.Name));
                            continue;
                        }

                        var noAncestry = effectiveCreationOptions == CreationOptions.IgnoreInheritance;
                        var onlyDirectAncestry = effectiveCreationOptions == CreationOptions.OnlyDirectInheritance;
                        var onlyAncestry = onlyDirectAncestry || effectiveCreationOptions == CreationOptions.OnlyInheritance;

                        ctorParameters.Add(localSourceObjects =>
                        {
                            var source = noAncestry ? null : GetSource(localSourceObjects, constructorParameterType, onlyDirectAncestry ? 1 : int.MaxValue);
                            return source ?? (onlyAncestry ? null : CreateObject(constructorParameterType, localSourceObjects));
                        });
                    }

                    var constructorDelegate = ctor.DelegateForCreateInstance();

                    constructor = localSources =>
                    {
                        if (localSources.Count >= this.RecursionLimit)
                        {
                            throw new InvalidOperationException($@"Recursion limit of {this.RecursionLimit} exceeded.");
                        }

                        var handleSingleton = this.structure.Singletons.Contains(objectType) || this.singletons.ContainsKey(objectType);

                        if (handleSingleton && this.singletons.TryGetValue(objectType, out object singleton))
                        {
                            // Register possible back references in singleton.
                            this.SetProperties(singleton, localSources, objectType, true);

                            return singleton;
                        }

                        if (++this.objectCount > this.ObjectLimit)
                        {
                            throw new InvalidOperationException($"Attempt to create more than {this.ObjectLimit} objects.");
                        }

                        var result = constructorDelegate(ctorParameters.Select(ca => ca(localSources)).ToArray());

                        this.createdObjects.Add(result);

                        this.SetProperties(result, localSources, objectType, false);

                        if (handleSingleton)
                        {
                            this.singletons[objectType] = result;
                        }

                        if (this.Logging)
                        {
                            this.loggingChain.Add(localSources.Concat(new[] { result }).ToList());
                        }

                        return result;
                    };
                }

                this.constructorCache.Add(objectType, constructor);
            }

            var newObject = constructor(sourceObjects);

            if (this.Logging && sourceObjects.Count == 0)
            {
                foreach (var chain in this.loggingChain.Reverse())
                {
                    var obj = chain.Last();
                    var pkstr = string.Join(", ", this.structure.Relations.GetPrimaryKeys(obj.GetType()).Select(k => k.GetMethod.Invoke(obj, new object[0]).ToString()));

                    var diag = $"{new string(' ', 4 * (chain.Count - 1))}{obj.GetType().Name} {pkstr}";
                    Console.WriteLine(diag);
                }
            }

            return newObject;
        }

        /// <summary>
        /// Gets an existing object of the given type from the sources list.
        /// </summary>
        /// <param name="sources">The existing object.</param>
        /// <param name="sourceType">The source type.</param>
        /// <param name="maxLevel">The maximum level.</param>
        /// <returns>An existing object or null.</returns>
        private object GetSource(IList<object> sources, Type sourceType, int maxLevel)
        {
            for (var i = sources.Count - 1; i >= 0 && maxLevel > 0; --i, --maxLevel)
            {
                if (sources[i].GetType() == sourceType)
                {
                    return sources[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Sets properties for the given input object, with the specified type.
        /// </summary>
        /// <param name="inputObject">The input object.</param>
        /// <param name="sourceObjects">The source object.</param>
        /// <param name="inputType">The type of the input object.</param>
        /// <param name="inputSingleton">Determines if the input object is a singleton.</param>
        private void SetProperties(object inputObject, IList<object> sourceObjects, Type inputType, bool inputSingleton)
        {
            if (!this.methodPropertyCache.TryGetValue(inputType, out var methods))
            {
                methods = new List<Action<object, IList<object>, bool>>();

                var referenceProperties = new List<PropertyInfo>();
                var propertyPlacement = new Dictionary<PropertyInfo, int>();

                foreach (var p in inputType.GetProperties())
                {
                    var property = p;
                    var propertyType = property.PropertyType;

                    if (!this.structure.PropertyCustomization.TryGetValue(property, out var effectiveCreationOptions))
                    {
                        if (!this.structure.TypeCustomization.TryGetValue(propertyType, out effectiveCreationOptions))
                        {
                            effectiveCreationOptions = CreationOptions.Default;
                        }
                    }

                    if (effectiveCreationOptions == CreationOptions.Skip)
                    {
                        // add nothing to methods.
                        continue;
                    }

                    if (this.structure.CustomPropertySetters.TryGetValue(property, out var valueFunc))
                    {
                        propertyPlacement.Add(property, methods.Count);
                        var setterDelegate = inputType.DelegateForSetPropertyValue(property.Name);
                        methods.Add((currentObject, _, currentSingleton) =>
                        {
                            if (!currentSingleton)
                            {
                                setterDelegate(currentObject, valueFunc(this));
                            }
                        });

                        continue;
                    }

                    var setter = this.ValueCreator(propertyType);
                    if (setter != null)
                    {
                        propertyPlacement.Add(property, methods.Count);
                        var setterDelegate = inputType.DelegateForSetPropertyValue(property.Name);
                        methods.Add((currentObject, _, currentSingleton) =>
                        {
                            if (!currentSingleton)
                            {
                                setterDelegate(currentObject, setter(property.Name));
                            }
                        });
                    }
                    else
                    {
                        referenceProperties.Add(property);
                    }
                }

                for (var pass = 1; pass <= 2; ++pass)
                {
                    // Pass 1, update pk id for 1:1 relation.
                    // Pass 2, the rest.
                    foreach (var p in referenceProperties)
                    {
                        var property = p;
                        var propertyType = property.PropertyType;
                        if (!this.structure.PropertyCustomization.TryGetValue(property, out var effectiveCreationOptions))
                        {
                            if (!this.structure.TypeCustomization.TryGetValue(propertyType, out effectiveCreationOptions))
                            {
                                effectiveCreationOptions = CreationOptions.Default;
                            }
                        }

                        var noAncestry = effectiveCreationOptions == CreationOptions.IgnoreInheritance;
                        var onlyDirectAncestry = effectiveCreationOptions == CreationOptions.OnlyDirectInheritance;
                        var onlyAncestry = onlyDirectAncestry || effectiveCreationOptions == CreationOptions.OnlyInheritance;

                        if (propertyType.IsGenericType)
                        {
                            if (propertyType.GetGenericTypeDefinition() == typeof(ICollection<>))
                            {
                                if (pass != 2)
                                {
                                    continue;
                                }

                                var elementType = propertyType.GetGenericArguments()[0];
                                var collectionType = typeof(HashSet<>).MakeGenericType(elementType);
                                var hashSetAdder = collectionType.DelegateForCallMethod(nameof(HashSet<int>.Add), elementType);
                                var hashSetCreator = collectionType.DelegateForCreateInstance();
                                var collectionSetter = inputType.DelegateForSetPropertyValue(property.Name);
                                var collectionGetter = inputType.DelegateForGetPropertyValue(property.Name);

                                methods.Add((currentObject, currentSources, currentSingleton) =>
                                {
                                    var collection = collectionGetter(currentObject);
                                    var adder = hashSetAdder;
                                    if (collection == null)
                                    {
                                        collection = hashSetCreator();
                                        collectionSetter(currentObject, collection);
                                    }
                                    else
                                    {
                                        var existingCollectionType = collection.GetType();

                                        if (existingCollectionType != collectionType)
                                        {
                                            adder = collection.GetType().DelegateForCallMethod("Add", elementType);
                                        }
                                    }

                                    var source = noAncestry ? null : GetSource(currentSources, elementType, 1);

                                    if (source != null)
                                    {
                                        adder(collection, source);
                                    }
                                    else
                                    {
                                        if (currentSingleton || onlyAncestry)
                                        {
                                            return;
                                        }

                                        if (!this.structure.Include.TryGetValue(property, out int elementCount))
                                        {
                                            elementCount = currentSources.Count == 0 ? this.RootCollectionMembers : this.NonRootCollectionMembers;
                                        }

                                        currentSources.Add(currentObject);

                                        for (var i = 0; i < elementCount; ++i)
                                        {
                                            adder(collection, CreateObject(elementType, currentSources));
                                        }

                                        currentSources.RemoveAt(currentSources.Count - 1);
                                    }
                                });
                            }
                            else
                            {
                                throw new InvalidOperationException("Unsupported type");
                            }
                        }
                        else
                        {
                            // Going from t to pt
                            // Note that primary keys and foreign keys may be equal.
                            var foreignKeyProps = this.structure.Relations.GetForeignKeys(inputType, propertyType);
                            var primaryKeyProps = this.structure.Relations.GetPrimaryKeys(inputType);

                            var pkFkEqual = foreignKeyProps.SequenceEqual(primaryKeyProps);

                            // Pass 1, only handle the case where the foreign key props and the primary key props are equal.
                            if (pkFkEqual)
                            {
                                if (pass == 2)
                                {
                                    continue;
                                }
                            }
                            else if (pass == 1)
                            {
                                continue;
                            }

                            var foreignKeySetDelegates = foreignKeyProps.Select(fkp => inputType.DelegateForSetPropertyValue(fkp.Name)).ToList();
                            var primaryKeyGetDelegates = primaryKeyProps.Select(pkp => propertyType.DelegateForGetPropertyValue(pkp.Name)).ToList();
                            var foreignObjectGetter = inputType.DelegateForGetPropertyValue(property.Name);
                            var foreignObjectSetter = inputType.DelegateForSetPropertyValue(property.Name);

                            var foreignKeyNullableGetDelegate = foreignKeyProps.Where(fkp => Nullable.GetUnderlyingType(fkp.PropertyType) != null).Select(fkp => inputType.DelegateForGetPropertyValue(fkp.Name)).FirstOrDefault();

                            methods.Add((currentObject, currentSources, currentSingleton) =>
                            {
                                // Handle nullable
                                object foreignObject = null;

                                if (foreignKeyNullableGetDelegate == null || foreignKeyNullableGetDelegate.Invoke(currentObject) != null)
                                {
                                    foreignObject = noAncestry ? null : GetSource(currentSources, propertyType, onlyDirectAncestry ? 1 : int.MaxValue);

                                    if (foreignObject == null && !onlyAncestry)
                                    {
                                        currentSources.Add(currentObject);

                                        foreignObject = this.CreateObject(propertyType, currentSources);

                                        currentSources.RemoveAt(currentSources.Count - 1);
                                    }
                                }

                                if (currentSingleton)
                                {
                                    var existing = foreignObjectGetter(currentObject);

                                    if (!ReferenceEquals(foreignObject, existing) && existing != null)
                                    {
                                        throw new InvalidOperationException($"Ambiguous property for singleton {inputType.Name}.{p.Name}.");
                                    }
                                }

                                if (foreignObject == null)
                                {
                                    // Clear nullable foreign keys
                                    if (foreignKeyNullableGetDelegate != null)
                                    {
                                        for (var i = 0; i < foreignKeyProps.Length; ++i)
                                        {
                                            foreignKeySetDelegates[i](currentObject, null);
                                        }
                                    }
                                }
                                else
                                {
                                    // Set foreign keys to primary keys of related object.
                                    for (var i = 0; i < foreignKeyProps.Length; ++i)
                                    {
                                        foreignKeySetDelegates[i](currentObject, primaryKeyGetDelegates[i](foreignObject));
                                    }

                                    foreignObjectSetter(currentObject, foreignObject);
                                }
                            });

                            if (!pkFkEqual)
                            {
                                for (var i = foreignKeyNullableGetDelegate == null ? 0 : 1; i < foreignKeyProps.Length; i++)
                                {
                                    var foreignKeyProp = foreignKeyProps[i];

                                    if (propertyPlacement.TryGetValue(foreignKeyProp, out var methodIndex))
                                    {
                                        methods[methodIndex] = null;
                                    }
                                }
                            }
                        }
                    }
                }

                methods = methods.Where(method => method != null).ToList();

                this.methodPropertyCache.Add(inputType, methods);
            }

            foreach (var method in methods)
            {
                method(inputObject, sourceObjects, inputSingleton);
            }
        }

        /// <summary>
        /// Returns a value creating delegate for the given type.
        /// </summary>
        /// <param name="t">The type for which to create values.</param>
        /// <returns></returns>
        private Func<string, object> ValueCreator(Type t)
        {
            if (this.structure.CustomConstructors.TryGetValue(t, out var creator))
            {
                return s => creator(this, s);
            }

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                t = Nullable.GetUnderlyingType(t);
                var inner = this.ValueCreator(t);
                if (inner == null)
                {
                    return null;
                }

                return s =>
                {
                    if (this.random.Next(0, 2) == 0)
                    {
                        return null;
                    }
                    else
                    {
                        return inner(s);
                    }
                };
            }

            if (t.IsEnum)
            {
                var values = Enum.GetValues(t);
                return _ => values.GetValue(this.random.Next(0, values.Length));
            }
            else if (t == typeof(string))
            {
                return s => this.CreateString(s, s.Length + 24);
            }
            else if (t == typeof(byte))
            {
                return _ => (byte)this.random.Next(byte.MinValue, byte.MaxValue + 1);
            }
            else if (t == typeof(short))
            {
                return _ => (short)this.random.Next(short.MinValue, short.MaxValue + 1);
            }
            else if (t == typeof(ushort))
            {
                return _ => (ushort)this.random.Next(ushort.MinValue, ushort.MaxValue + 1);
            }
            else if (t == typeof(int))
            {
                return _ => this.random.Next(int.MinValue, int.MaxValue); // TODO capped at maxValue - 1
            }
            else if (t == typeof(uint))
            {
                return _ => (uint)this.random.Next(int.MinValue, int.MaxValue); // TODO capped at maxValue - 1
            }
            else if (t == typeof(long))
            {
                return _ =>
                {
                    var rnd = new byte[8];
                    this.random.NextBytes(rnd);
                    return BitConverter.ToInt64(rnd, 0);
                };
            }
            else if (t == typeof(ulong))
            {
                return _ =>
                {
                    var rnd = new byte[8];
                    this.random.NextBytes(rnd);
                    return BitConverter.ToUInt64(rnd, 0);
                };
            }
            else if (t == typeof(double))
            {
                return _ => (this.random.NextDouble() - 0.5) * double.MaxValue;
            }
            else if (t == typeof(float))
            {
                return _ => (float)(this.random.NextDouble() - 0.5) * float.MaxValue;
            }
            else if (t == typeof(decimal))
            {
                return _ => Math.Round(100 * (decimal)(this.random.NextDouble() - 0.5) * 1e2m) / 100; // TODO look into overflows?
            }
            else if (t == typeof(bool))
            {
                return _ => this.random.Next(0, 2) == 1;
            }
            else if (t == typeof(Guid))
            {
                return _ =>
                {
                    var rnd = new byte[16];
                    this.random.NextBytes(rnd);
                    return new Guid(rnd);
                };
            }
            else if (t == typeof(DateTime))
            {
                return _ => DateTime.Now + TimeSpan.FromMilliseconds((this.random.NextDouble() - 0.5) * 62e9);
            }
            else if (t == typeof(DateTimeOffset))
            {
                return _ => DateTimeOffset.Now + TimeSpan.FromMilliseconds((this.random.NextDouble() - 0.5) * 62e9);
            }

            return null;
        }
    }
}
