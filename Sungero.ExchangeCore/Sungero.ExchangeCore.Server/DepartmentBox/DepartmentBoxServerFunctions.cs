using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.DepartmentBox;

namespace Sungero.ExchangeCore.Server
{
  partial class DepartmentBoxFunctions
  {
    /// <summary>
    /// Создать ящик подразделения.
    /// </summary>
    /// <param name="box">Абонентский ящик нашей организации.</param>
    public static void CreateDepartmentBoxes(IBusinessUnitBox box)
    {
      var createdDepartments = DepartmentBoxes.GetAll(d => Equals(d.RootBox, box)).ToList();
      var newDepartmentBoxes = new List<IDepartmentBox>();
      var changedDepartmentBoxes = new List<IDepartmentBox>();
      var deletedDepartmentBoxes = new List<IDepartmentBox>();
      var client = Functions.BusinessUnitBox.GetClient(box);
      
      // Обходим кэширование клиента DCX для получения актуальной оргструктуры.
      var allDepartments = client.GetOrganizationStructure(client.OurSubscriber.CounteragentId);
      var serviceHead = allDepartments.SingleOrDefault(d => d.IsHead);
      var departments = allDepartments.Where(d => !d.IsHead);
      
      // Создание / обновление абонентских ящиков подразделений.
      foreach (var department in departments)
      {
        var departmentBox = createdDepartments.SingleOrDefault(d => Equals(d.ServiceId, department.Id)) ?? DepartmentBoxes.Create();
        if (!Equals(departmentBox.ServiceId, department.Id))
          departmentBox.ServiceId = department.Id;
        if (!Equals(departmentBox.RootBox, box))
          departmentBox.RootBox = box;
        if (!Equals(departmentBox.ServiceName, department.Name))
        {
          if (!string.IsNullOrEmpty(departmentBox.Name))
            changedDepartmentBoxes.Add(departmentBox);
          departmentBox.ServiceName = department.Name;
        }
        
        if (!departmentBox.AccessRights.IsGranted(DefaultAccessRightsTypes.Change, box.Responsible))
          departmentBox.AccessRights.Grant(box.Responsible, DefaultAccessRightsTypes.Change);
        var exchangeRole = Sungero.ExchangeCore.PublicFunctions.Module.GetExchangeServiceUsersRole();
        if (!exchangeRole.AccessRights.IsGranted(DefaultAccessRightsTypes.Change, box.Responsible))
        {
          exchangeRole.AccessRights.Grant(box.Responsible, DefaultAccessRightsTypes.Change);
          exchangeRole.Save();
        }
        
        if (!createdDepartments.Contains(departmentBox))
        {
          createdDepartments.Add(departmentBox);
          newDepartmentBoxes.Add(departmentBox);
        }
      }
      
      // Проверка иерархии.
      foreach (var department in departments)
      {
        var departmentBox = createdDepartments.Single(d => Equals(d.ServiceId, department.Id));
        var parentBox = createdDepartments.SingleOrDefault(d => Equals(d.ServiceId, department.ParentDepartmentId));
        if (department.ParentDepartmentId == null ||
            department.ParentDepartmentId == Guid.Empty.ToString() ||
            (serviceHead != null && department.ParentDepartmentId == serviceHead.Id))
        {
          if (!Equals(departmentBox.ParentBox, box))
            departmentBox.ParentBox = box;
        }
        else if (!Equals(departmentBox.ParentBox, parentBox))
          departmentBox.ParentBox = parentBox;
      }
      
      // Закрытие удаленных ящиков.
      foreach (var department in createdDepartments)
      {
        var exists = departments.Any(d => Equals(d.Id, department.ServiceId));
        if (!exists && (department.IsDeleted == false || department.Status != Status.Closed))
        {
          department.IsDeleted = true;
          department.Status = Status.Closed;
          deletedDepartmentBoxes.Add(department);
        }
      }
      
      // Сохранение одной сущности может потянуть за собой остальные, так что парамсы обновляем до первого сохранения.
      foreach (var department in createdDepartments)
        ((Domain.Shared.IExtendedEntity)department).Params[Constants.BoxBase.DisableSaveValidation] = true;
      
      foreach (var department in createdDepartments.Where(x => x.State.IsChanged))
        department.Save();
      
      if (newDepartmentBoxes.Any() || changedDepartmentBoxes.Any() || deletedDepartmentBoxes.Any())
        CreateDepartmentBoxNotice(box, newDepartmentBoxes, changedDepartmentBoxes, deletedDepartmentBoxes);
    }
    
    /// <summary>
    /// Получить ответственного за ящик.
    /// </summary>
    /// <returns>Ответственный.</returns>
    public override Sungero.Company.IEmployee GetResponsible()
    {
      return _obj.Responsible != null && _obj.Status == ExchangeCore.DepartmentBox.Status.Active ?
        _obj.Responsible :
        Functions.BoxBase.GetResponsible(_obj.ParentBox);
    }
    
    /// <summary>
    /// Получить срок задания на обработку.
    /// </summary>
    /// <param name="assignee">Исполнитель задания.</param>
    /// <returns>Срок задания на обработку документа из сервиса обмена.</returns>
    public override DateTime GetProcessingTaskDeadline(IEmployee assignee)
    {
      return _obj.Responsible != null && _obj.Status == ExchangeCore.DepartmentBox.Status.Active ?
        base.GetProcessingTaskDeadline(assignee) :
        Functions.BoxBase.GetProcessingTaskDeadline(_obj.ParentBox, assignee);
    }
    
    /// <summary>
    /// Получить ответственного за документ.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="infos">Список информаций о документе.</param>
    /// <returns>Ответственный.</returns>
    [Public]
    public override Sungero.Company.IEmployee GetExchangeDocumentResponsible(Sungero.Parties.ICounterparty counterparty, List<Sungero.Exchange.IExchangeDocumentInfo> infos)
    {
      var company = Parties.CompanyBases.As(counterparty);
      if (_obj.Routing == ExchangeCore.BoxBase.Routing.CPResponsible && company != null && company.Responsible != null)
        return _obj.Status == ExchangeCore.DepartmentBox.Status.Active ? company.Responsible : Functions.BoxBase.GetExchangeDocumentResponsible(_obj.ParentBox, counterparty, infos);

      return ExchangeCore.PublicFunctions.BoxBase.GetResponsible(_obj);
    }
    
    /// <summary>
    /// Получить сервис обмена ящика.
    /// </summary>
    /// <returns>Сервис обмена.</returns>
    public override IExchangeService GetExchangeService()
    {
      return _obj.RootBox.ExchangeService;
    }
    
    /// <summary>
    /// Получить НОР ящика.
    /// </summary>
    /// <returns>Наша организация.</returns>
    [Public]
    public override Sungero.Company.IBusinessUnit GetBusinessUnit()
    {
      return _obj.RootBox.BusinessUnit;
    }

    /// <summary>
    /// Создать уведомление об изменении оргструктуры абонентского ящика нашей организации.
    /// </summary>
    /// <param name="box">Абонентский ящик нашей организации.</param>
    /// <param name="newDepartmentBoxes">Новые ящики подразделений.</param>
    /// <param name="changedDepartmentBoxes">Измененные ящики подразделений.</param>
    /// <param name="deletedDepartmentBoxes">Удаленные ящики подразделений.</param>
    private static void CreateDepartmentBoxNotice(IBusinessUnitBox box, List<IDepartmentBox> newDepartmentBoxes, List<IDepartmentBox> changedDepartmentBoxes, List<IDepartmentBox> deletedDepartmentBoxes)
    {
      var task = Workflow.SimpleTasks.Create();
      var dateWithUTC = Sungero.Docflow.PublicFunctions.Module.GetDateWithUTCLabel(Calendar.Now);
      var subject = DepartmentBoxes.Resources.DepartmentBoxNoticeSubjectFormat(box.Name, dateWithUTC);
      task.Subject = Exchange.PublicFunctions.Module.CutText(subject, task.Info.Properties.Subject.Length);
      var step = task.RouteSteps.AddNew();
      step.AssignmentType = Workflow.SimpleTask.AssignmentType.Notice;
      step.Performer = box.Responsible;
      
      if (newDepartmentBoxes.Any())
      {
        task.Subject = DepartmentBoxes.Resources.DepartmentBoxAssignmentSubjectFormat(box.Name, dateWithUTC);
        step.AssignmentType = Workflow.SimpleTask.AssignmentType.Assignment;
        task.Deadline = Calendar.Now.AddWorkingDays(step.Performer, 1);
        task.NeedsReview = false;
        DepartmentBoxSection(task, newDepartmentBoxes, DepartmentBoxes.Resources.NewDepartmentBoxesNotice);
      }
      
      if (changedDepartmentBoxes.Any())
        DepartmentBoxSection(task, changedDepartmentBoxes, DepartmentBoxes.Resources.ChangedDepartmentBoxesNotice);

      if (deletedDepartmentBoxes.Any())
      {
        DepartmentBoxSection(task, deletedDepartmentBoxes, DepartmentBoxes.Resources.DeletedDepartmentBoxesNotice);
        
        // Уведомление ответственным о закрытии ящика подразделения.
        foreach (var responsible in deletedDepartmentBoxes.Where(b => b.Responsible != null && !Equals(b.Responsible, box.Responsible)).Select(b => b.Responsible))
        {
          var deleteBoxTask = Workflow.SimpleTasks.Create();
          deleteBoxTask.Subject = DepartmentBoxes.Resources.DeletedDepartmentBoxesReponsibleNotice;
          var deleteBoxStep = deleteBoxTask.RouteSteps.AddNew();
          deleteBoxStep.AssignmentType = Workflow.SimpleTask.AssignmentType.Notice;
          deleteBoxStep.Performer = responsible;
          var responsibleBoxes = deletedDepartmentBoxes.Where(b => Equals(b.Responsible, responsible));
          
          deleteBoxTask.ActiveText += DepartmentBoxes.Resources.DeletedDepartmentBoxesNotice + Environment.NewLine;
          foreach (var departmentBox in responsibleBoxes)
          {
            deleteBoxTask.Attachments.Add(departmentBox);
            deleteBoxTask.ActiveText += Constants.BusinessUnitBox.Delimiter;
            deleteBoxTask.ActiveText += Hyperlinks.Get(departmentBox);
            deleteBoxTask.ActiveText += Environment.NewLine;
          }
          deleteBoxTask.ActiveText += Environment.NewLine;
          deleteBoxTask.Save();
          deleteBoxTask.Start();
        }
        
      }
      
      task.Save();
      task.Start();
    }

    /// <summary>
    /// Добавить секцию в текст уведомления об изменениях оргструктуры абонентского ящика нашей организации.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="departmentBoxes">Ящики подразделений.</param>
    /// <param name="sectionHeader">Заголовок секции.</param>
    private static void DepartmentBoxSection(Workflow.ISimpleTask task, List<IDepartmentBox> departmentBoxes, string sectionHeader)
    {
      task.ActiveText += sectionHeader + Environment.NewLine;
      foreach (var departmentBox in departmentBoxes)
      {
        task.Attachments.Add(departmentBox);
        task.ActiveText += Constants.BusinessUnitBox.Delimiter;
        task.ActiveText += Hyperlinks.Get(departmentBox);
        task.ActiveText += Environment.NewLine;
      }
      task.ActiveText += Environment.NewLine;
    }
    
    /// <summary>
    /// Получить подразделение ящика.
    /// </summary>
    /// <returns>Подразделение.</returns>
    [Public]
    public override Company.IDepartment GetDepartment()
    {
      return _obj.Status == ExchangeCore.DepartmentBox.Status.Active ?
        _obj.Department :
        Functions.BoxBase.GetDepartment(_obj.ParentBox);
    }
  }
}