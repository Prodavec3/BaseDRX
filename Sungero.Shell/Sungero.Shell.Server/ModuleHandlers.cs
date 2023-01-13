using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.RecordManagement;

namespace Sungero.Shell.Server
{
  partial class OnAcquaintanceFolderHandlers
  {

    public virtual IQueryable<Sungero.Workflow.IAssignmentBase> OnAcquaintanceDataQuery(IQueryable<Sungero.Workflow.IAssignmentBase> query)
    {
      var result = query.Where(t => Sungero.RecordManagement.AcquaintanceAssignments.Is(t));
      
      // Запрос непрочитанных без фильтра.
      if (_filter == null)
        return RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result);
      
      // Фильтры по статусу, замещению и периоду.
      return RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result, _filter.InProcess,
                                                                                 _filter.Last30Days, _filter.Last90Days,
                                                                                 _filter.Last180Days, false);
    }

    public virtual bool IsOnAcquaintanceVisible()
    {
      return false;
    }
  }

  partial class OnVerificationFolderHandlers
  {

    public virtual IQueryable<Sungero.Workflow.IAssignmentBase> OnVerificationDataQuery(IQueryable<Sungero.Workflow.IAssignmentBase> query)
    {
      var result = query.Where(t => Sungero.SmartProcessing.VerificationAssignments.Is(t));
      
      // Запрос непрочитанных без фильтра.
      if (_filter == null)
        return RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result);
      
      // Фильтры по статусу, замещению и периоду.
      return RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result, _filter.InProcess,
                                                                                 _filter.Last30Days, _filter.Last90Days,
                                                                                 _filter.Last180Days, false);
    }

    public virtual bool IsOnVerificationVisible()
    {
      return false;
    }
  }

  partial class ExchangeDocumentProcessingFolderHandlers
  {

    public virtual bool IsExchangeDocumentProcessingVisible()
    {
      return ExchangeCore.BoxBases.GetAll().Any(x => (Equals(x.Responsible, Company.Employees.Current) && x.Routing == ExchangeCore.BoxBase.Routing.BoxResponsible) ||
                                                (x.Routing == ExchangeCore.BoxBase.Routing.CPResponsible && Parties.CompanyBases.GetAll().Any(c => Equals(c.Responsible, Company.Employees.Current))));
    }

    public virtual IQueryable<Sungero.Workflow.IAssignmentBase> ExchangeDocumentProcessingDataQuery(IQueryable<Sungero.Workflow.IAssignmentBase> query)
    {
      var result = query.Where(t => Exchange.ExchangeDocumentProcessingAssignments.Is(t) ||
                               ExchangeCore.IncomingInvitationAssignments.Is(t) ||
                               ExchangeCore.CounterpartyConflictProcessingAssignments.Is(t));
      
      if (_filter == null)
        return RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result);
      
      // Фильтры по статусу и периоду.
      result = RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result, _filter.InProcess,
                                                                                   _filter.Last30Days, _filter.Last90Days, _filter.Last180Days, false);
      
      return result;
    }
  }

  partial class ApprovalFolderHandlers
  {

    public virtual IQueryable<Sungero.Workflow.ITask> ApprovalDataQuery(IQueryable<Sungero.Workflow.ITask> query)
    {
      // Фильтр по типу.
      var typeFilterEnabled = _filter != null && (_filter.RuleBased || _filter.Free);
      var showRuleBasedApproval = !typeFilterEnabled || _filter.RuleBased;
      var showFreeApproval = !typeFilterEnabled || _filter.Free;
      var result = query
        .Where(t => showRuleBasedApproval && ApprovalTasks.Is(t) ||
               showFreeApproval && FreeApprovalTasks.Is(t));
      
      if (_filter == null)
        return RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result);
      
      // Фильтры по статусу и периоду.
      result = RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result, _filter.InProcess,
                                                                                   _filter.Last30Days, _filter.Last90Days, _filter.Last180Days, false);
      
      return result;
    }

    public virtual bool IsApprovalVisible()
    {
      return !Docflow.PublicFunctions.Module.IncludedInBusinessUnitHeadsRole();
    }
  }

  partial class NoticesFolderHandlers
  {

    public virtual IQueryable<Sungero.Workflow.INotice> NoticesDataQuery(IQueryable<Sungero.Workflow.INotice> query)
    {
      // TODO: не забыть поставить ограничение (180).
      // Фильтр по статусу.
      if (_filter == null)
        return query;
      
      if (_filter.IsNotRead)
        return query.Where(n => n.IsRead != true);

      // Фильтр по периоду.
      var filterDate = Calendar.Now;
      if (_filter.Last30Days)
        filterDate = Calendar.Today.AddDays(-30);
      else if (_filter.Last90Days)
        filterDate = Calendar.Today.AddDays(-90);
      else if (_filter.Last180Days)
        filterDate = Calendar.Today.AddDays(-180);
      
      return query.Where(n => n.Created >= filterDate);
    }
  }

  partial class OnRegisterFolderHandlers
  {

    public virtual IQueryable<Sungero.Workflow.IAssignmentBase> OnRegisterDataQuery(IQueryable<Sungero.Workflow.IAssignmentBase> query)
    {
      // Фильтр по типу.
      var result = query;
      result = Functions.Module.GetSpecificAssignmentsWithCollapsed(result, Docflow.ApprovalReviewAssignmentCollapsedStagesTypesRe.StageType.Register);
      
      // Запрос непрочитанных без фильтра.
      if (_filter == null)
        return RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result);
      
      // Фильтры по статусу, замещению и периоду.
      result = RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result, _filter.InProcess,
                                                                                   _filter.Last30Days, _filter.Last90Days, _filter.Last180Days, false);
      
      return result;
    }

    public virtual bool IsOnRegisterVisible()
    {
      return !(Docflow.PublicFunctions.Module.IncludedInBusinessUnitHeadsRole() ||
               Docflow.PublicFunctions.Module.IncludedInDepartmentManagersRole()) && Docflow.PublicFunctions.Module.Remote.IncludedInClerksRole();
    }
  }

  partial class OnReworkFolderHandlers
  {

    public virtual IQueryable<Sungero.Workflow.IAssignmentBase> OnReworkDataQuery(IQueryable<Sungero.Workflow.IAssignmentBase> query)
    {
      // Фильтр по типу.
      var result = query
        .Where(a => ApprovalReworkAssignments.Is(a) || FreeApprovalReworkAssignments.Is(a) ||
               (ReportRequestAssignments.Is(a) && (ReportRequestAssignments.As(a).IsRework == true)));
      
      // Запрос непрочитанных без фильтра.
      if (_filter == null)
        return RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result);
      
      // Фильтры по статусу, замещению и периоду.
      result = RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result, _filter.InProcess,
                                                                                   _filter.Last30Days, _filter.Last90Days, _filter.Last180Days, false);
      
      return result;
    }

    public virtual bool IsOnReworkVisible()
    {
      return !(Docflow.PublicFunctions.Module.IncludedInBusinessUnitHeadsRole() ||
               Docflow.PublicFunctions.Module.IncludedInDepartmentManagersRole() ||
               Docflow.PublicFunctions.Module.Remote.IncludedInClerksRole());
    }
  }

  partial class OnPrintFolderHandlers
  {

    public virtual IQueryable<Sungero.Workflow.IAssignmentBase> OnPrintDataQuery(IQueryable<Sungero.Workflow.IAssignmentBase> query)
    {
      // Фильтр по типу.
      var result = query;
      result = Functions.Module.GetSpecificAssignmentsWithCollapsed(result, Docflow.ApprovalReviewAssignmentCollapsedStagesTypesRe.StageType.Print);
      
      // Запрос непрочитанных без фильтра.
      if (_filter == null)
        return RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result);
      
      // Фильтры по статусу, замещению и периоду.
      result = RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result, _filter.InProcess,
                                                                                   _filter.Last30Days, _filter.Last90Days, _filter.Last180Days, false);
      
      return result;
    }

    public virtual bool IsOnPrintVisible()
    {
      return !(Docflow.PublicFunctions.Module.IncludedInBusinessUnitHeadsRole() ||
               Docflow.PublicFunctions.Module.IncludedInDepartmentManagersRole()) && Docflow.PublicFunctions.Module.Remote.IncludedInClerksRole();
    }
  }

  partial class OnChekingFolderHandlers
  {

    public virtual IQueryable<Sungero.Workflow.IAssignmentBase> OnChekingDataQuery(IQueryable<Sungero.Workflow.IAssignmentBase> query)
    {
      // Фильтр по типу.
      var typeFilterEnabled = _filter != null && (_filter.ActionItem || _filter.Other);
      var showActionItems = !typeFilterEnabled || _filter.ActionItem;
      var showOthers = !typeFilterEnabled || _filter.Other;
      
      var result = query
        .Where(a => showActionItems && ActionItemSupervisorAssignments.Is(a) ||
               showOthers && (Docflow.DeadlineExtensionAssignments.Is(a) ||
                              RecordManagement.DeadlineExtensionAssignments.Is(a) ||
                              ReportRequestCheckAssignments.Is(a) ||
                              Workflow.ReviewAssignments.Is(a) ||
                              CheckReturnCheckAssignments.Is(a)));
      
      // Запрос непрочитанных без фильтра.
      if (_filter == null)
        return RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result);
      
      // Фильтры по статусу, замещению и периоду.
      result = RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result, _filter.InProcess,
                                                                                   _filter.Last30Days, _filter.Last90Days, _filter.Last180Days, false);
      
      return result;
    }

    public virtual bool IsOnChekingVisible()
    {
      return Docflow.PublicFunctions.Module.IncludedInBusinessUnitHeadsRole() ||
        Docflow.PublicFunctions.Module.IncludedInDepartmentManagersRole() ||
        Docflow.PublicFunctions.Module.Remote.IncludedInClerksRole();
    }
  }

  partial class OnApprovalFolderHandlers
  {

    public virtual IQueryable<Sungero.Workflow.IAssignmentBase> OnApprovalDataQuery(IQueryable<Sungero.Workflow.IAssignmentBase> query)
    {
      // Фильтр по типу.
      var typeFilterEnabled = _filter != null && (_filter.RuleBased || _filter.Free);
      var showRuleBasedApproval = !typeFilterEnabled || _filter.RuleBased;
      var showFreeApproval = !typeFilterEnabled || _filter.Free;
      var result = query
        .Where(a => showRuleBasedApproval && (ApprovalAssignments.Is(a) || ApprovalManagerAssignments.Is(a)) ||
               showFreeApproval && FreeApprovalAssignments.Is(a));
      
      // Запрос непрочитанных без фильтра.
      if (_filter == null)
        return RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result);
      
      // Фильтры по статусу, замещению и периоду.
      result = RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result, _filter.InProcess,
                                                                                   _filter.Last30Days, _filter.Last90Days, _filter.Last180Days, false);
      
      return result;
    }

    public virtual bool IsOnApprovalVisible()
    {
      return Docflow.PublicFunctions.Module.IncludedInBusinessUnitHeadsRole() ||
        Docflow.PublicFunctions.Module.IncludedInDepartmentManagersRole() ||
        !Docflow.PublicFunctions.Module.Remote.IncludedInClerksRole();
    }
  }

  partial class OnDocumentProcessingFolderHandlers
  {

    public virtual IQueryable<Sungero.Workflow.IAssignmentBase> OnDocumentProcessingDataQuery(IQueryable<Sungero.Workflow.IAssignmentBase> query)
    {
      // Фильтр по типу.
      var typeFilterEnabled = _filter != null && (_filter.ProcessResolution ||
                                                  _filter.ConfirmSigning ||
                                                  _filter.SendActionItem ||
                                                  _filter.Send ||
                                                  _filter.CheckReturn ||
                                                  _filter.Other);
      
      var stageTypes = new List<Sungero.Core.Enumeration>();
      if (!typeFilterEnabled || _filter.ProcessResolution)
        stageTypes.Add(Docflow.ApprovalReviewAssignmentCollapsedStagesTypesRe.StageType.ReviewingResult);
      if (!typeFilterEnabled || _filter.ConfirmSigning)
        stageTypes.Add(Docflow.ApprovalReviewAssignmentCollapsedStagesTypesRe.StageType.ConfirmSign);
      if (!typeFilterEnabled || _filter.SendActionItem)
        stageTypes.Add(Docflow.ApprovalReviewAssignmentCollapsedStagesTypesRe.StageType.Execution);
      if (!typeFilterEnabled || _filter.Send)
        stageTypes.Add(Docflow.ApprovalReviewAssignmentCollapsedStagesTypesRe.StageType.Sending);
      
      var showExecution = !typeFilterEnabled || _filter.SendActionItem;
      var showCheckReturn = !typeFilterEnabled || _filter.CheckReturn;
      var showOther = !typeFilterEnabled || _filter.Other;
      
      var result = query.Where(q =>
                               // Рассмотрение.
                               ApprovalReviewAssignments.Is(q) && ApprovalReviewAssignments.As(q).CollapsedStagesTypesRe.Any(s => stageTypes.Contains(s.StageType.Value)) ||
                               // Подписание.
                               ApprovalSigningAssignments.Is(q) && ApprovalSigningAssignments.As(q).CollapsedStagesTypesSig.Any(s => stageTypes.Contains(s.StageType.Value)) ||
                               // Создание поручений.
                               (ApprovalExecutionAssignments.Is(q) && ApprovalExecutionAssignments.As(q).CollapsedStagesTypesExe.Any(s => stageTypes.Contains(s.StageType.Value)) ||
                                showExecution && ReviewResolutionAssignments.Is(q)) ||
                               // Подготовка проекта резолюции.
                               showExecution && PreparingDraftResolutionAssignments.Is(q) ||
                               // Регистрация.
                               ApprovalRegistrationAssignments.Is(q) && ApprovalRegistrationAssignments.As(q).CollapsedStagesTypesReg.Any(s => stageTypes.Contains(s.StageType.Value)) ||
                               // Печать.
                               ApprovalPrintingAssignments.Is(q) && ApprovalPrintingAssignments.As(q).CollapsedStagesTypesPr.Any(s => stageTypes.Contains(s.StageType.Value)) ||
                               // Отправка контрагенту.
                               ApprovalSendingAssignments.Is(q) && ApprovalSendingAssignments.As(q).CollapsedStagesTypesSen.Any(s => stageTypes.Contains(s.StageType.Value)) ||
                               // Контроль возврата.
                               showCheckReturn && ApprovalCheckReturnAssignments.Is(q) ||
                               // Прочие задания.
                               showOther && (ApprovalSimpleAssignments.Is(q) || ApprovalCheckingAssignments.Is(q) || ReviewReworkAssignments.Is(q)));
      
      // Запрос непрочитанных без фильтра.
      if (_filter == null)
        return RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result);
      
      // Фильтры по статусу, замещению и периоду.
      result = RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result, _filter.InProcess,
                                                                                   _filter.Last30Days, _filter.Last90Days, _filter.Last180Days, false);
      return result;
    }

    public virtual bool IsOnDocumentProcessingVisible()
    {
      return !(Docflow.PublicFunctions.Module.IncludedInBusinessUnitHeadsRole() ||
               Docflow.PublicFunctions.Module.IncludedInDepartmentManagersRole()) && Docflow.PublicFunctions.Module.Remote.IncludedInClerksRole();
    }
  }

  partial class OnSigningFolderHandlers
  {

    public virtual IQueryable<Sungero.Workflow.IAssignmentBase> OnSigningDataQuery(IQueryable<Sungero.Workflow.IAssignmentBase> query)
    {
      // Фильтр по типу.
      var result = query
        .Where(a => Docflow.ApprovalSigningAssignments.Is(a) && Docflow.ApprovalSigningAssignments.As(a).IsConfirmSigning != true);
      
      // Запрос непрочитанных без фильтра.
      if (_filter == null)
        return RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result);
      
      // Фильтры по статусу, замещению и периоду.
      result = RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result, _filter.InProcess,
                                                                                   _filter.Last30Days, _filter.Last90Days, _filter.Last180Days, false);
      
      return result;
    }

    public virtual bool IsOnSigningVisible()
    {
      return Docflow.PublicFunctions.Module.IncludedInBusinessUnitHeadsRole();
    }
  }

  partial class OnReviewFolderHandlers
  {

    public virtual IQueryable<Sungero.Workflow.IAssignmentBase> OnReviewDataQuery(IQueryable<Sungero.Workflow.IAssignmentBase> query)
    {
      // Фильтр по типу.
      var typeFilterEnabled = _filter != null && (_filter.Incoming || _filter.Inner);
      var showIncoming = !typeFilterEnabled || _filter.Incoming;
      var showInner = !typeFilterEnabled || _filter.Inner;
      var result = query
        .Where(a => showIncoming && (RecordManagement.ReviewManagerAssignments.Is(a) ||
                                     RecordManagement.PreparingDraftResolutionAssignments.Is(a) ||
                                     RecordManagement.ReviewDraftResolutionAssignments.Is(a) ||
                                     Docflow.ApprovalReviewAssignments.Is(a) &&
                                     Docflow.ApprovalTasks.As(a.Task).ApprovalRule.DocumentFlow == Docflow.ApprovalRuleBase.DocumentFlow.Incoming) ||
               showInner && Docflow.ApprovalReviewAssignments.Is(a) &&
               Docflow.ApprovalTasks.As(a.Task).ApprovalRule.DocumentFlow != Docflow.ApprovalRuleBase.DocumentFlow.Incoming);
      
      // Запрос непрочитанных без фильтра.
      if (_filter == null)
        return RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result);
      
      // Фильтры по статусу, замещению и периоду.
      result = RecordManagement.PublicFunctions.Module.ApplyCommonSubfolderFilters(result, _filter.InProcess,
                                                                                   _filter.Last30Days, _filter.Last90Days, _filter.Last180Days, false);
      
      return result;
    }

    public virtual bool IsOnReviewVisible()
    {
      return Docflow.PublicFunctions.Module.IncludedInBusinessUnitHeadsRole();
    }
  }

  partial class ShellHandlers
  {
  }
}