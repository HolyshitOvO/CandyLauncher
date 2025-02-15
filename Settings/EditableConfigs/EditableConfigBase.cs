﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains;
using ReflectSettings.Attributes;
using ReflectSettings.EditableConfigs.InheritingAttribute;

namespace ReflectSettings.EditableConfigs
{
    public abstract class EditableConfigBase<T> : IEditableConfig
    {
        private readonly IList<Attribute> _attributes;
        private ChangeTrackingManager _changeTrackingManager;
        private object _additionalData;

        protected SettingsFactory Factory { get; }

        public object ForInstance { get; set; }

        public PropertyInfo PropertyInfo { get; set; }

        private object _lastSetValue;

        public object Value
        {
            get => GetValue();
            set
            {
                var newValue = ParseValue(value);
                if (Equals(newValue, Value))
                    return;
                var oldValue = Value;
                _lastSetValue = newValue;
                SetValue(newValue);
                ValueChanged?.Invoke(this, new EditableConfigValueChangedEventArgs(oldValue, Value));
                OnPropertyChanged();
            }
        }

        public void ValueWasExternallyChanged()
        {
            // check whether the value did in fact externally change.
            if (Value == _lastSetValue)
                return;

            // check whether current value is legal. Or in other words, the value would change when parsed again
            var parsed = ParseValue(Value);
            if (!Equals(parsed, Value))
            {
                Value = Value;
            }
            else
            {
                _lastSetValue = Value;
                ValueChanged?.Invoke(this, new EditableConfigValueChangedEventArgs(_lastSetValue, Value));
                OnPropertyChanged(nameof(Value));
            }
        }

        public string HashCode => GetHashCode().ToString();

        protected abstract T ParseValue(object value);

        private T GetValue() => (T) PropertyInfo.GetValue(ForInstance);

        private void SetValue(T value) => PropertyInfo.SetValue(ForInstance, value);

        protected EditableConfigBase(object forInstance, PropertyInfo propertyInfo, SettingsFactory factory, ChangeTrackingManager changeTrackingManager)
        {
            ForInstance = forInstance;
            PropertyInfo = propertyInfo;
            Factory = factory;
            ChangeTrackingManager = changeTrackingManager;

            _attributes = propertyInfo.GetCustomAttributes(true).OfType<Attribute>().ToList();
            InitCalculatedAttributes();
            UpdateCalculatedValues();

            PredefinedValues.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(PredefinedValues));
        }

        private TAttribute Attribute<TAttribute>() where TAttribute : Attribute =>
            _attributes.OfType<TAttribute>().FirstOrDefault() ?? Activator.CreateInstance<TAttribute>();

        private IEnumerable<TAttribute> Attributes<TAttribute>() where TAttribute : Attribute =>
            _attributes.OfType<TAttribute>();

        protected MinMaxAttribute MinMax() => Attribute<MinMaxAttribute>();

        public ObservableCollection<object> PredefinedValues { get; } = new ObservableCollection<object>();
        // todo: 完善key名 和显示名分离
        public ObservableCollection<object> PredefinedNames { get; } = new ObservableCollection<object>();

        public bool HasPredefinedValues => _attributes.OfType<PredefinedValuesAttribute>().Any() ||
                                           CalculatedValues.ForThis.Any() ||
                                           CalculatedValuesAsync.ForThis.Any() ||
                                           PropertyInfo.PropertyType.IsEnum;
        
        public bool IsColorString => _attributes.OfType<ColorString>().Any() ||
                                           CalculatedValues.ForThis.Any() ||
                                           CalculatedValuesAsync.ForThis.Any() ||
                                           PropertyInfo.PropertyType.IsEnum;
        
        public bool IsShortcutKeyString => _attributes.OfType<ShortcutKeyString>().Any() ||
                                           CalculatedValues.ForThis.Any() ||
                                           CalculatedValuesAsync.ForThis.Any() ||
                                           PropertyInfo.PropertyType.IsEnum;
        
        public bool IsFilePathString => _attributes.OfType<FilePathString>().Any() ||
                                           CalculatedValues.ForThis.Any() ||
                                           CalculatedValuesAsync.ForThis.Any() ||
                                           PropertyInfo.PropertyType.IsEnum;
        
        public bool HasCalculatedType => CalculatedTypes.ForThis.Any();

        public ChangeTrackingManager ChangeTrackingManager
        {
            get => _changeTrackingManager;
            private set
            {
                _changeTrackingManager?.Remove(this);

                _changeTrackingManager = value;

                _changeTrackingManager?.Add(this);
            }
        }

        public bool IsDisplayNameProperty => _attributes.OfType<IsDisplayName>().FirstOrDefault() != null;

        public virtual void UpdateCalculatedValues()
        {
            OnPropertyChanged(nameof(IsHidden));
            if (!HasPredefinedValues && !HasCalculatedType)
                return;

            UpdatePredefinedType();

            if (CalculatedValuesAsync.ForThis.Any())
            {
                IsBusy = true;
                return;
            }

            var existingValues = PredefinedValues.ToList();
            var newValuesOfT = GetPredefinedValues().ToList();
            var newValues = newValuesOfT.OfType<object>().ToList();
            if (newValuesOfT.Any(x => x == null))
                newValues.Add(null);

            var toRemove = existingValues.Except(newValues);
            var toAdd = newValues.Except(existingValues);

            var somethingChanged = false;
            foreach (var value in toAdd)
            {
                PredefinedValues.Add(value);
                somethingChanged = true;
            }

            if (somethingChanged)
            {
                Value = Value;
                somethingChanged = false;
            }

            foreach (var value in toRemove)
            {
                PredefinedValues.Remove(value);
                somethingChanged = true;
            }

            var valueTypeDiffersFromPredefined = Value?.GetType() != GetPredefinedType();


            if (somethingChanged || (HasCalculatedType && valueTypeDiffersFromPredefined))
            {
                Value = Value;
            }
        }

        private bool _loadingValuesAsync;

        public async Task UpdateCalculatedValuesAsync()
        {
            if (!CalculatedValuesAsync.ForThis.Any())
                return;

            IsBusy = true;
            _loadingValuesAsync = true;

            var existingValues = PredefinedValues.ToList();
            var newValues = GetPredefinedValues().OfType<object>().ToList();

            foreach (var asyncValues in CalculatedValuesAsync.ForThis)
            {
                var result = await asyncValues.ResolveValuesAsync(CalculatedValuesAsync.Inherited);
                newValues = newValues.Concat(result).ToList();
            }

            _loadingValuesAsync = false;

            var toRemove = existingValues.Except(newValues);
            var toAdd = newValues.Except(existingValues);

            var somethingChanged = false;
            foreach (var value in toAdd)
            {
                PredefinedValues.Add(value);
                somethingChanged = true;
            }

            if (somethingChanged)
            {
                Value = Value;
                somethingChanged = false;
            }

            foreach (var value in toRemove)
            {
                PredefinedValues.Remove(value);
                somethingChanged = true;
            }

            var valueTypeDiffersFromPredefined = Value?.GetType() != GetPredefinedType();

            if (somethingChanged || (HasCalculatedType && valueTypeDiffersFromPredefined))
            {
                Value = Value;
            }

            IsBusy = false;
        }


        private void InitCalculatedAttributes()
        {
            foreach (var attribute in _attributes.OfType<INeedsInstance>())
            {
                attribute.AttachedToInstance = ForInstance;
            }

            CalculatedVisibility =
                new InheritedAttributes<CalculatedVisibilityAttribute>(_attributes.OfType<CalculatedVisibilityAttribute>());
            CalculatedValues =
                new InheritedAttributes<CalculatedValuesAttribute>(_attributes.OfType<CalculatedValuesAttribute>());
            CalculatedTypes =
                new InheritedAttributes<CalculatedTypeAttribute>(_attributes.OfType<CalculatedTypeAttribute>());
            CalculatedValuesAsync =
                new InheritedAttributes<CalculatedValuesAsyncAttribute>(_attributes
                    .OfType<CalculatedValuesAsyncAttribute>());
        }

        public InheritedAttributes<CalculatedVisibilityAttribute> CalculatedVisibility { get; private set; }

        public InheritedAttributes<CalculatedTypeAttribute> CalculatedTypes { get; private set; }

        public InheritedAttributes<CalculatedValuesAttribute> CalculatedValues { get; private set; }

        public InheritedAttributes<CalculatedValuesAsyncAttribute> CalculatedValuesAsync { get; private set; }

        private IEnumerable<T> GetPredefinedValues()
        {
            var staticValues = Attribute<PredefinedValuesAttribute>();
            // methods with a key are only used when the specific key is used as the resolution name of the attribute
            var calculatedValuesAttributes = CalculatedValues.ForThis;

            var calculatedValues =
                calculatedValuesAttributes.SelectMany(x => x.ResolveValues(CalculatedValues.Inherited));

            var concat = staticValues.Values.Concat(calculatedValues).ToList();
            var toReturn = concat.OfType<T>().Except(ForbiddenValues()).ToList();

            if (concat.Any(x => x == null))
            {
                toReturn.Add(default);
            }

            return toReturn;
        }

        /// <summary>
        /// 获取文件选择的筛选扩展的值
        /// </summary>
        /// <returns></returns>
        public string GetFilePathSelectorFilterValues()
        {
            var staticValues = Attribute<FilePathString>();
            // methods with a key are only used when the specific key is used as the resolution name of the attribute
            var calculatedValuesAttributes = CalculatedValues.ForThis;

            var calculatedValues =
                calculatedValuesAttributes.SelectMany(x => x.ResolveValues(CalculatedValues.Inherited));

            var concat = staticValues.Values.Concat(calculatedValues).ToList();
            var toReturn = concat.OfType<T>().Except(ForbiddenValues()).ToList();

            if (concat.Any(x => x == null))
            {
                toReturn.Add(default);
            }

            if (toReturn.Count == 0)
            {
                return "";
            }
            else if (toReturn[0] is string)
            {
                return toReturn[0] as string;
            }
            else
            {
                return "";
            }
        }

        protected Type GetPredefinedType()
        {
            if (_predefinedType == null)
                UpdatePredefinedType();

            return _predefinedType;
        }

        private Type _predefinedType;

        private void UpdatePredefinedType()
        {
            // methods with a key are only used when the specific key is used as the resolution name of the attribute
            var calculatedTypeAttributes = CalculatedTypes.ForThis;

            var calculatedTypes =
                calculatedTypeAttributes.Select(x => x.CallMethod(CalculatedTypes.Inherited, ForInstance));

            _predefinedType = calculatedTypes.FirstOrDefault(IsAssignableToT) ?? typeof(T);
        }

        private bool IsAssignableToT(Type type) => typeof(T).IsAssignableFrom(type) && type != typeof(object);

        protected object InstantiateObjectOfSpecificType(Type type)
        {
            var method = GetType().GetMethod(nameof(InstantiateObject));
            if (method == null)
                return default;

            var asGeneric = method.MakeGenericMethod(type);

            return asGeneric.Invoke(this, null);
        }

        public TObject InstantiateObject<TObject>()
        {
            var targetType = typeof(TObject);
            var typeToInstantiate = targetType;

            if (targetType == typeof(string) && "" is TObject stringAsTObject)
                return stringAsTObject;

            if (targetType.IsPrimitive)
                return default;

            if (targetType.IsInterface)
                typeToInstantiate = PossibleTypesFor(targetType).First();

            if (!HasConstructorWithNoParameter(typeToInstantiate))
                return default;

            return (TObject) Activator.CreateInstance(typeToInstantiate);
        }

        private bool HasConstructorWithNoParameter(Type type) => type.GetConstructor(Type.EmptyTypes) != null;

        protected IEnumerable<Type> PossibleTypesFor(Type interfaceType)
        {
            var typesForInstantiation = Attribute<TypesForInstantiationAttribute>().Types;
            return typesForInstantiation.Where(interfaceType.IsAssignableFrom);
        }

        protected IEnumerable<T> ForbiddenValues()
        {
            var forbiddenValues = Attribute<ForbiddenValuesAttribute>().ForbiddenValues;

            return forbiddenValues.OfType<T>();
        }

        protected bool IsValueAllowed(T value)
        {
            // while possible values are loaded, allow anything (so the set value doesn't get lost due to this method saying no
            if (_loadingValuesAsync)
                return true;

            if (value == null)
                return false;

            var isTypeAllowed = GetPredefinedType().IsInstanceOfType(value);

            var predefinedValues = PredefinedValues;
            var isPredefinedValueOrNoPredefinedValuesGiven =
                predefinedValues.Count == 0 || predefinedValues.Any(v => Equals(v, value));

            var isValueAllowed = isPredefinedValueOrNoPredefinedValuesGiven && isTypeAllowed;
            var isValueForbidden = ForbiddenValues().Any(v => Equals(v, value));


            return isValueAllowed && !isValueForbidden;
        }

        protected bool IsNumericValueAllowed(dynamic numericValue)
        {
            // while possible values are loaded, allow anything (so the set value doesn't get lost due to this method saying no
            if (_loadingValuesAsync)
                return true;

            var minMax = MinMax();

            if (!(numericValue is T asT))
                return false;

            if (!IsValueAllowed(asT))
                return false;

            return numericValue >= minMax.Min && numericValue <= minMax.Max;
        }

        protected bool TryCastNumeric<TNumeric>(object value, out TNumeric result)
        {
            try
            {
                var castMethod = CastMethod<TNumeric>();
                if (castMethod != null)
                    result = (TNumeric) castMethod(value is string asString ? asString.Replace(',', '.') : value);
                else
                    result = (TNumeric) value;
                return true;
            }
            catch (Exception)
            {
                result = default;
                return false;
            }
        }

        private Func<object, object> CastMethod<TNumeric>()
        {
            var type = typeof(TNumeric);

            if (type == typeof(double))
                return x => Convert.ToDouble(x, CultureInfo.InvariantCulture);

            if (type == typeof(int))
                return x => Convert.ToInt32(x, CultureInfo.InvariantCulture);

            if (type == typeof(float))
                return x => (float) Convert.ToDouble(x, CultureInfo.InvariantCulture);

            return x => x;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<EditableConfigValueChangedEventArgs> ValueChanged;

        public string DisplayName => ResolveDisplayName();

        public bool IsHidden => _attributes.OfType<IsHiddenAttribute>().Any() || CalculatedVisibility.ForThis.Any(x => x.IsHidden(CalculatedVisibility.Inherited));

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        public object AdditionalData
        {
            get => _additionalData;
            set
            {
                _additionalData = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///  获取设置项显示的标题名称 todo 写一个summary
        /// </summary>
        /// <returns></returns>
        protected virtual string ResolveDisplayName()
        {
            string displayName = Attribute<ConfigTitle>().ConfigTitleName ??
                                 _attributes.OfType<NameAttribute>().FirstOrDefault()?.Name ?? PropertyInfo.Name;
            Debug.WriteLine(displayName);
            return displayName;
        }

        public string KeyName => GetKeyName();

        protected virtual string GetKeyName()
        {
            return _attributes.OfType<NameAttribute>().FirstOrDefault()?.Name ?? PropertyInfo.Name;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsRemoving { get; set; }
        

	}
}
