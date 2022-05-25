using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Contracts;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.ContractsUI.Server
{
  public class ModuleFunctions
  {
    #region Виджеты
    
    /// <summary>
    /// Получить все договоры и доп. соглашения в стадии согласования, где Я ответственный.
    /// </summary>
    /// <param name="query">Запрос виджета.</param>
    /// <param name="substitution">Параметр "Учитывать замещения".</param>
    /// <param name="show">Параметр "Показывать".</param>
    /// <returns>Список договоров и доп. соглашений.</returns>
    public IQueryable<Sungero.Contracts.IContractualDocument> GetMyContractualDocuments(IQueryable<Sungero.Contracts.IContractualDocument> query,
                                                                                        bool substitution,
                                                                                        Enumeration show)
    {
      query = query.Where(cd => ContractBases.Is(cd) || SupAgreements.Is(cd));
      
      // Проверить статус жизненного цикла.
      query = query.Where(cd => cd.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Draft)
        .Where(cd => cd.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.OnApproval ||
               cd.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.OnRework ||
               cd.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.PendingSign ||
               cd.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.Signed ||
               cd.ExternalApprovalState == Docflow.OfficialDocument.ExternalApprovalState.OnApproval);
      
      // Если показывать надо не все Договоры, то дофильтровываем по ответственному.
      if (show != Sungero.ContractsUI.Widgets.MyContracts.Show.All)
      {
        var currentEmployee = Company.Employees.Current;
        var responsibleEmployees = new List<IUser>();
        if (currentEmployee != null)
          responsibleEmployees.Add(currentEmployee);
        
        var employeeDepartment = currentEmployee != null ? currentEmployee.Department : null;
        
        // Учитывать замещения, если выставлен соответствующий параметр.
        if (substitution)
        {
          var substitutions = Substitutions.ActiveSubstitutedUsersWithoutSystem;
          responsibleEmployees.AddRange(substitutions);
        }
        
        // Если выбрано значение параметра "Договоры подразделения", то добавляем к списку ответственных всех сотрудников подразделения.
        if (show == Sungero.ContractsUI.Widgets.MyContracts.Show.Department)
        {
          var departmentsEmployees = Company.Employees.GetAll(e => Equals(e.Department, employeeDepartment));
          responsibleEmployees.AddRange(departmentsEmployees);
        }

        query = query.Where(cd => responsibleEmployees.Contains(cd.ResponsibleEmployee));
      }
      
      return query;
    }
    
    /// <summary>
    /// Получить все договоры и доп. соглашения на завершении, где Я ответственный.
    /// </summary>
    /// <param name="query">Запрос виджета.</param>
    /// <param name="needAutomaticRenewal">Признак "С пролонгацией".</param>
    /// <param name="substitution">Параметр "Учитывать замещения".</param>
    /// <param name="show">Параметр "Показывать".</param>
    /// <returns>Список договоров и доп. соглашений на завершении.</returns>
    public IQueryable<Sungero.Contracts.IContractualDocument> GetMyExpiringSoonContracts(IQueryable<Sungero.Contracts.IContractualDocument> query,
                                                                                         bool? needAutomaticRenewal,
                                                                                         bool substitution,
                                                                                         Enumeration show)
    {
      var today = Calendar.UserToday;
      var lastDate = today.AddDays(14);
      // Если показывать надо не все Договоры, то дофильтровываем по ответственному.
      if (show != Sungero.ContractsUI.Widgets.MyContracts.Show.All)
      {
        var currentEmployee = Employees.Current;
        var responsibleEmployees = new List<IUser>();
        if (currentEmployee != null)
          responsibleEmployees.Add(currentEmployee);
        
        var employeeDepartment = currentEmployee != null ? currentEmployee.Department : null;
        
        // Учитывать замещения, если выставлен соответствующий параметр.
        if (substitution)
        {
          var substitutions = Substitutions.ActiveSubstitutedUsersWithoutSystem;
          responsibleEmployees.AddRange(substitutions);
        }
        
        // Если выбрано значение параметра "Договоры подразделения", то добавляем к списку ответственных всех сотрудников подразделения.
        if (show == Sungero.ContractsUI.Widgets.MyContracts.Show.Department)
        {
          var departmentsEmployees = Company.Employees.GetAll(e => Equals(e.Department, employeeDepartment));
          responsibleEmployees.AddRange(departmentsEmployees);
        }
        
        query = query.Where(cd => responsibleEmployees.Contains(cd.ResponsibleEmployee));
      }
      query = query
        .Where(q => ContractBases.Is(q) || SupAgreements.Is(q))
        .Where(q => q.LifeCycleState == Sungero.Contracts.SupAgreement.LifeCycleState.Active);
      
      query = query.Where(q => q.ValidTill.HasValue)
        .Where(q => (ContractBases.Is(q) && today.AddDays(ContractBases.As(q).DaysToFinishWorks ?? 14) >= q.ValidTill) ||
               (SupAgreements.Is(q) && q.ValidTill.Value <= lastDate))
        .Where(q => SupAgreements.Is(q) || ContractBases.Is(q) && (ContractBases.As(q).DaysToFinishWorks == null ||
                                                                   ContractBases.As(q).DaysToFinishWorks <= Docflow.PublicConstants.Module.MaxDaysToFinish));

      // Признак с автопролонгацией у договоров.
      if (needAutomaticRenewal.HasValue)
        query = query.Where(q => ContractBases.Is(q) &&
                            ContractBases.As(q).IsAutomaticRenewal.HasValue &&
                            ContractBases.As(q).IsAutomaticRenewal.Value == needAutomaticRenewal.Value);
      
      return query;
    }

    #endregion
    
    /// <summary>
    /// Отфильтровать действующие виды документов с документопотоком "Договоры".
    /// </summary>
    /// <param name="query">Фильтруемые виды документов.</param>
    /// <param name="withoutActs">True, если получить наследников договоров и доп. соглашений. Иначе - все договорные виды документов.</param>
    /// <returns>Виды документов.</returns>
    [Public]
    public static IQueryable<Docflow.IDocumentKind> ContractsFilterContractsKind(IQueryable<Docflow.IDocumentKind> query, bool withoutActs)
    {
      query = query
        .Where(d => d.Status == CoreEntities.DatabookEntry.Status.Active)
        .Where(d => d.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Contracts);
      
      if (withoutActs)
      {
        var supKinds = Docflow.PublicFunctions.DocumentKind.GetAvailableDocumentKinds(typeof(ISupAgreement));
        var contractKinds = Docflow.PublicFunctions.DocumentKind.GetAvailableDocumentKinds(typeof(IContractBase));
        query = query.Where(k => supKinds.Contains(k) || contractKinds.Contains(k));
      }
      
      return query;
    }
  }
}