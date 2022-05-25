using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalRole;

namespace Sungero.Docflow.Server
{
  partial class ApprovalRoleFunctions
  {
    /// <summary>
    /// Получить сотрудника из роли.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Сотрудник.</returns>
    public override IEmployee GetRolePerformer(IApprovalTask task)
    {
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.Addressee)
        return this.GetPerformerAddressee(task);
      
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.AddrAssistant)
        return this.GetPerformerAddresseeAssistant(task);
      
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.DocRegister)
        return this.GetPerformerRegister(task);
      
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.OutDocRegister)
        return this.GetPerformerOutDocumentRegister(task);
      
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.Initiator)
        return this.GetPerformerInitiator(task);
      
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.InitManager)
        return this.GetPerformerInitiatorManager(task);
      
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.PrintResp)
        return this.GetPerformerPrintResponsible(task);
      
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.SignAssistant)
        return this.GetPerformerSignatoryAssistant(task);
      
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.Signatory)
        return this.GetPerformerSignatory(task);
      
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.CompResponsible)
        return this.GetPerformerCompanyResponsible(task);
      
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.DepartManager)
        return this.GetPerformerDepartmentManager(task);
      
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.DocDepManager)
        return this.GetPerformerDocumentDepartmentManager(task);
      return base.GetRolePerformer(task);
    }
    
    /// <summary>
    /// Получить сотрудников роли "Согласующие".
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="additionalApprovers">Доп.согласующие.</param>
    /// <returns>Список сотрудников.</returns>
    public override List<IEmployee> GetApproversRolePerformers(IApprovalTask task, List<Sungero.CoreEntities.IRecipient> additionalApprovers)
    {
      var result = new List<IEmployee>();
      
      if (task != null)
      {
        // Обязательные согласующие.
        var recipients = task.ReqApprovers.Select(a => a.Approver).ToList();
        
        // Доп.согласующие.
        if (additionalApprovers == null)
          if (task.Status == Workflow.Task.Status.Draft || task.Status == Workflow.Task.Status.Aborted)
            additionalApprovers = task.AddApprovers.Select(a => a.Approver).ToList();
          else
            additionalApprovers = task.AddApproversExpanded.Select(a => a.Approver).ToList();
        recipients.AddRange(additionalApprovers);
        result.AddRange(Company.PublicFunctions.Module.GetEmployeesFromRecipients(recipients));
        
        return result;
      }
      
      return base.GetApproversRolePerformers(task, additionalApprovers);
    }
    
    public override List<IEmployee> GetAddresseesRolePerformers(IApprovalTask task)
    {
      if (task != null)
        return task.Addressees.Select(a => a.Addressee).ToList();
      
      return base.GetAddresseesRolePerformers(task);
    }
    
    #region Вычисление ролей
    
    /// <summary>
    /// Получить адресата.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Сотрудник.</returns>
    private IEmployee GetPerformerAddressee(IApprovalTask task)
    {
      return task.Addressee;
    }
    
    /// <summary>
    /// Получить помощника адресата.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Сотрудник.</returns>
    private IEmployee GetPerformerAddresseeAssistant(IApprovalTask task)
    {
      var addressee = this.GetPerformerAddressee(task);
      return Functions.Module.GetSecretary(addressee) ?? addressee;
    }
    
    /// <summary>
    /// Получить регистратора.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Сотрудник.</returns>
    private IEmployee GetPerformerRegister(IApprovalTask task)
    {
      return Functions.Module.GetClerk(task.DocumentGroup.OfficialDocuments.FirstOrDefault()) ?? this.GetPerformerInitiator(task);
    }
    
    /// <summary>
    /// Получить регистратора исходящих документов.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Сотрудник.</returns>
    private IEmployee GetPerformerOutDocumentRegister(IApprovalTask task)
    {
      var document = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
      return Functions.Module.GetClerk(document, Docflow.DocumentKind.DocumentFlow.Outgoing) ?? this.GetPerformerInitiator(task);
    }
    
    /// <summary>
    /// Получить инициатора согласования.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Сотрудник.</returns>
    private IEmployee GetPerformerInitiator(IApprovalTask task)
    {
      return Employees.As(task.Author);
    }
    
    /// <summary>
    /// Получить руководителя инициатора согласования.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Сотрудник.</returns>
    private IEmployee GetPerformerInitiatorManager(IApprovalTask task)
    {
      return Functions.Module.GetManager(task.Author);
    }
    
    /// <summary>
    /// Получить руководителя подразделения инициатора согласования.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Сотрудник.</returns>
    private IEmployee GetPerformerDepartmentManager(IApprovalTask task)
    {
      var department = Functions.Module.GetDepartment(task.Author);
      return department != null ? department.Manager : null;
    }
    
    /// <summary>
    /// Получить руководителя подразделения, указанного в документе.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Сотрудник.</returns>
    private IEmployee GetPerformerDocumentDepartmentManager(IApprovalTask task)
    {
      var document = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var manager = document != null && document.Department != null ? document.Department.Manager : null;
      return manager == null ? this.GetPerformerDepartmentManager(task) : manager;
    }
    
    /// <summary>
    /// Получить сотрудника, ответственного за печать.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Сотрудник.</returns>
    private IEmployee GetPerformerPrintResponsible(IApprovalTask task)
    {
      var stages = Functions.ApprovalTask.GetStages(task).Stages;
      
      var reviewStageIndex = Functions.ApprovalStage.GetIndexOfStage(task, Docflow.ApprovalStage.StageType.Review, stages);
      var signStageIndex = Functions.ApprovalStage.GetIndexOfStage(task, Docflow.ApprovalStage.StageType.Sign, stages);
      var printStageIndex = Functions.ApprovalStage.GetIndexOfStage(task, Docflow.ApprovalStage.StageType.Print, stages);
      
      // Для печати перед подписанием или рассмотрением исполнителем взять помощника.
      var performer = Employees.Null;
      if (printStageIndex < signStageIndex)
      {
        performer = Functions.Module.GetSecretary(this.GetPerformerSignatory(task));
      }
      else if (printStageIndex < reviewStageIndex)
      {
        performer = Functions.Module.GetSecretary(this.GetPerformerAddressee(task));
      }
      
      if (performer == null)
        performer = Functions.Module.GetClerk(task.DocumentGroup.OfficialDocuments.FirstOrDefault());
      
      return performer ?? this.GetPerformerInitiator(task);
    }
    
    /// <summary>
    /// Получить помощника подписывающего.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Сотрудник.</returns>
    private IEmployee GetPerformerSignatoryAssistant(IApprovalTask task)
    {
      var signatory = this.GetPerformerSignatory(task);
      return Functions.Module.GetSecretary(signatory) ?? signatory;
    }
    
    /// <summary>
    /// Получить подписывающего.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Сотрудник.</returns>
    private IEmployee GetPerformerSignatory(IApprovalTask task)
    {
      return Functions.Module.GetPerformerSignatory(task);
    }
    
    /// <summary>
    /// Получить ответственного за контрагента.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Сотрудник.</returns>
    private IEmployee GetPerformerCompanyResponsible(IApprovalTask task)
    {
      var performer = this.GetPerformerInitiator(task);
      var document = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
      {
        var counterparties = Docflow.PublicFunctions.OfficialDocument.GetCounterparties(document);
        if (counterparties != null && counterparties.Count == 1)
        {
          var company = Parties.Companies.As(counterparties.FirstOrDefault());
          if (company != null && company.Responsible != null)
            performer = company.Responsible;
        }
      }
      
      return performer;
    }
    
    #endregion
  }
}