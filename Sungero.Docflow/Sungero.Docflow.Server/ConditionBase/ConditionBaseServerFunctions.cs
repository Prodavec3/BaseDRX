using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ConditionBase;

namespace Sungero.Docflow.Server
{
  partial class ConditionBaseFunctions
  {
    /// <summary>
    /// Сформировать часть заголовка условия по выбору из multiselect.
    /// </summary>
    /// <param name="mainSet">Список выбранных значений.</param>
    /// <returns>Форматированная строка для заголовка.</returns>
    /// <remarks>На данный момент актуально для условий типа Addressee и Currency.</remarks>
    public static string ConditionMultiSelectNameBuilder(List<string> mainSet)
    {
      var lastItem = mainSet.Last();
      
      mainSet.Remove(lastItem);
      
      if (!mainSet.Any())
        return lastItem;
      
      return string.Format("{0}{1}{2}", string.Join(Conditions.Resources.TitleJoinSeparator, mainSet),
                           Conditions.Resources.TitleLastJoinSeparator,
                           lastItem);
    }
    
    /// <summary>
    /// Получить условие по id.
    /// </summary>
    /// <param name="id">Id условия.</param>
    /// <returns>Условие.</returns>
    [Public, Remote(IsPure = true)]
    public static IConditionBase GetCondition(int id)
    {
      return ConditionBases.GetAll().Where(c => c.Id == id).FirstOrDefault();
    }
    
    /// <summary>
    /// Форматировать сумму.
    /// </summary>
    /// <param name="amount">Сумма.</param>
    /// <returns>Форматированная сумма.</returns>
    [Public]
    public static string AmountFormat(double? amount)
    {
      if (amount == null)
        return string.Empty;
      
      return amount.Value.ToString("N");
    }
    
    /// <summary>
    /// Удалить условие.
    /// </summary>
    /// <param name="condition">Условие.</param>
    [Remote, Public]
    public static void DeleteCondition(IConditionBase condition)
    {
      if (condition != null)
        ConditionBases.Delete(condition);
    }
    
    /// <summary>
    /// Проверить, не используется ли условие в правилах, по которым есть задачи в работе.
    /// </summary>
    /// <param name="condition">Проверяемое условие.</param>
    /// <returns>True, если используется, false, если нет.</returns>
    [Remote(IsPure = true), Public]
    public static bool HasRules(IConditionBase condition)
    {
      return ApprovalRuleBases.GetAll(r => r.Conditions.Any(s => Equals(s.Condition, condition)) && r.Status == Docflow.ApprovalRuleBase.Status.Active).Any();
    }
    
    /// <summary>
    /// Сравнить исполнителя двух ролей.
    /// </summary>
    /// <param name="task">Задача на согласование по регламенту.</param>
    /// <returns>Структура с результатом сравнения.</returns>
    [Remote(IsPure = true)]
    public virtual Structures.ConditionBase.ConditionResult CompareRoles(IApprovalTask task)
    {
      var employee = Functions.ApprovalRoleBase.GetRolePerformer(_obj.ApprovalRole, task);
      var employeeForComparison = Functions.ApprovalRoleBase.GetRolePerformer(_obj.ApprovalRoleForComparison, task);
      if (employee == null && employeeForComparison == null)
        return Structures.ConditionBase.ConditionResult.Create(null, ConditionBases.Resources.CannotComputeRolesFormat(_obj.Name));
      return Structures.ConditionBase.ConditionResult.Create(Equals(employee, employeeForComparison), string.Empty);
    }
    
    /// <summary>
    /// Сравнить исполнителя роли и сотрудника.
    /// </summary>
    /// <param name="task">Задача на согласование по регламенту.</param>
    /// <returns>Структура с результатом сравнения.</returns>
    [Remote(IsPure = true)]
    public virtual Structures.ConditionBase.ConditionResult CompareRoleAndRecipient(IApprovalTask task)
    {
      var employee = Functions.ApprovalRoleBase.GetRolePerformer(_obj.ApprovalRole, task);
      var employeeForComparison = Functions.ApprovalRuleBase.GetEmployeeByAssignee(_obj.RecipientForComparison);
      return Structures.ConditionBase.ConditionResult.Create(Equals(employee, employeeForComparison), string.Empty);
    }
    
    /// <summary>
    /// Проверить, что сотрудник входит в роль согласования.
    /// </summary>
    /// <param name="task">Задача на согласование по регламенту.</param>
    /// <returns>Структура с результатом проверки.</returns>
    [Remote(IsPure = true)]
    public virtual Structures.ConditionBase.ConditionResult CheckEmployeeInRole(IApprovalTask task)
    {
      var employee = Functions.ApprovalRuleBase.GetEmployeeByAssignee(_obj.RecipientForComparison);
      var employeesInRole = Functions.ApprovalRoleBase.GetRolePerformers(_obj.ApprovalRole, task);
      return Structures.ConditionBase.ConditionResult.Create(employeesInRole.Contains(employee), string.Empty);
    }
    
    /// <summary>
    /// Получить текст условия.
    /// </summary>
    /// <returns>Текст условия.</returns>
    public virtual string GetConditionName()
    {
      using (TenantInfo.Culture.SwitchTo())
      {
        if (_obj.ConditionType == ConditionType.AmountIsMore)
          return ConditionBases.Resources.ContractIsMoreThanFormat(_obj.Info.Properties.AmountOperator.GetLocalizedValue(_obj.AmountOperator),
                                                                   Functions.ConditionBase.AmountFormat(_obj.Amount));
        
        if (_obj.ConditionType == ConditionType.Nonresident)
          return ConditionBases.Resources.NonResidentCounterparty;
        
        if (_obj.ConditionType == ConditionType.ProjectDocument)
          return ConditionBases.Resources.HasProject;
        
        if (_obj.ConditionType == ConditionType.Currency)
          return ConditionBases.Resources.ContractCurrencyFormat(Functions.ConditionBase.ConditionMultiSelectNameBuilder(_obj.Currencies.Select(c => c.Currency.Name).ToList()));
        
        if (_obj.ConditionType == ConditionType.DocumentKind)
        {
          var documentKinds = _obj.ConditionDocumentKinds.Select(c => c.DocumentKind.Name);
          return ConditionBases.Resources.ConditionDocumentKindFormat(Functions.ConditionBase.ConditionMultiSelectNameBuilder(documentKinds.ToList()));
        }
        
        if (_obj.ConditionType == ConditionType.RolesComparer)
          return ConditionBases.Resources.RoleComparerFormat(_obj.ApprovalRole.Name, _obj.ApprovalRoleForComparison.Name);

        if (_obj.ConditionType == ConditionType.RoleEmpComparer || _obj.ConditionType == ConditionType.EmployeeInRole)
        {
          var recipientName = _obj.RecipientForComparison.Name;
          if (Company.Employees.Is(_obj.RecipientForComparison))
            recipientName = Company.Employees.As(_obj.RecipientForComparison).Person.ShortName;
          return ConditionBases.Resources.RoleComparerFormat(_obj.ApprovalRole.Name, recipientName);
        }
        
        if (_obj.ConditionType == ConditionType.DeliveryMethod)
        {
          var deliveryMethod = _obj.DeliveryMethods.Select(c => c.DeliveryMethod.Name);
          var conditionMultiSelectName = Functions.ConditionBase.ConditionMultiSelectNameBuilder(deliveryMethod.ToList());
          return ConditionBases.Resources.ConditionDeliveryMethodFormat(conditionMultiSelectName);
        }
        
        if (_obj.ConditionType == ConditionType.SignedByCParty)
          return ConditionBases.Resources.SignedByCounterparty;
        
        if (_obj.ConditionType == ConditionType.HasAddenda)
          return ConditionBases.Resources.HasAddendaFormat(_obj.AddendaDocumentKind.Name);
        
        return string.Empty;
      }
    }
    
    /// <summary>
    /// Получить сотрудников из условия.
    /// </summary>
    /// <returns>Список сотрудников.</returns>
    public virtual List<Sungero.Company.IEmployee> GetEmployeesFromProperties()
    {
      var employees = new List<Sungero.Company.IEmployee>();
      if (_obj.RecipientForComparison != null)
      {
        var employee = Functions.ApprovalRuleBase.GetEmployeeByAssignee(_obj.RecipientForComparison);
        if (employee != null)
          employees.Add(employee);
      }
      return employees;
    }
  }
}