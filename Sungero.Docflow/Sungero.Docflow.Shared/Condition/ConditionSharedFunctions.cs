using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Condition;

namespace Sungero.Docflow.Shared
{
  partial class ConditionFunctions
  {
    /// <summary>
    /// Получить словарь поддерживаемых типов условий.
    /// </summary>
    /// <returns>
    /// Словарь.
    /// Ключ - GUID типа документа.
    /// Значение - список поддерживаемых условий.
    /// </returns>
    public override System.Collections.Generic.Dictionary<string, List<Enumeration?>> GetSupportedConditions()
    {
      var baseConditions = base.GetSupportedConditions();
      
      // Бухгалтерские документы.
      var accountingTypes = Functions.DocumentKind.GetDocumentGuids(typeof(IAccountingDocumentBase));
      foreach (var type in accountingTypes)
        baseConditions[type].AddRange(new List<Enumeration?> { Sungero.Docflow.Condition.ConditionType.AmountIsMore,
                                        Sungero.Docflow.Condition.ConditionType.Currency,
                                        Sungero.Docflow.Condition.ConditionType.Nonresident });
      
      // Служебная записка.
      baseConditions[Docflow.Constants.Module.MemoTypeGuid].Add(Sungero.Docflow.Condition.ConditionType.Addressee);
      baseConditions[Docflow.Constants.Module.MemoTypeGuid].Add(Sungero.Docflow.Condition.ConditionType.ManyAddressees);
      
      return baseConditions;
    }

    public override void ChangePropertiesAccess()
    {
      base.ChangePropertiesAccess();
      
      var isAddressee = _obj.ConditionType == ConditionType.Addressee;
      
      _obj.State.Properties.Addressees.IsVisible = isAddressee;
      _obj.State.Properties.Addressees.IsRequired = isAddressee;
    }
    
    public override void ClearHiddenProperties()
    {
      base.ClearHiddenProperties();
      
      if (!_obj.State.Properties.Addressees.IsVisible)
        _obj.Addressees.Clear();
    }
    
    /// <summary>
    /// Проверить условие.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>True, если условие выполняется, и false, если не выполняется.</returns>
    public override Structures.ConditionBase.ConditionResult CheckCondition(IOfficialDocument document, IApprovalTask task)
    {
      if (_obj.ConditionType == ConditionType.Addressee)
        return this.CheckAddressee(document, task);
      
      if (_obj.ConditionType == ConditionType.ManyAddressees)
        return this.CheckManyAddresses(document, task);
      
      return base.CheckCondition(document, task);
    }
    
    /// <summary>
    /// Проверить адресата.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>True, если адресат совпадает.</returns>
    private Structures.ConditionBase.ConditionResult CheckAddressee(IOfficialDocument document, IApprovalTask task)
    {
      if (Memos.Is(document))
      {
        var addressee = Memos.As(document).Addressee;
        if (addressee != null)
          return Structures.ConditionBase.ConditionResult.Create(_obj.Addressees.Any(c => Equals(c.Addressee, addressee)), string.Empty);
        else
          return Structures.ConditionBase.ConditionResult.Create(null, Conditions.Resources.MemoAddresseeIsEmpty);
      }
      
      return Structures.ConditionBase.ConditionResult.Create(null, Conditions.Resources.CheckAddresseePossibleOnlyToMemo);
    }
    
    /// <summary>
    /// Проверить признак наличия нескольких адресатов.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>True, если адресатов несколько.</returns>
    private Structures.ConditionBase.ConditionResult CheckManyAddresses(IOfficialDocument document, IApprovalTask task)
    {
      if (ApprovalTasks.Is(task))
      {
        var manyAddressees = ApprovalTasks.As(task).IsManyAddressees;
        return Structures.ConditionBase.ConditionResult.Create(manyAddressees == true, string.Empty);
      }
      
      return Structures.ConditionBase.ConditionResult.Create(null, Conditions.Resources.CheckManyAddresseesPossibleOnlyToMemo);
    }
  }
}