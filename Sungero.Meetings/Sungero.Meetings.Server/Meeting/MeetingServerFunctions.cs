using System;
using System.Collections.Generic;
using System.Linq;
using CommonLibrary;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Meetings.Meeting;

namespace Sungero.Meetings.Server
{
  partial class MeetingFunctions
  {
    /// <summary>
    /// Создать совещание.
    /// </summary>
    /// <returns>Совещание.</returns>
    [Remote, Public]
    public static IMeeting CreateMeeting()
    {
      return Meetings.Create();
    }
    
    /// <summary>
    /// Получить совещание, в контексте которого создается документ.
    /// </summary>
    /// <returns>Совещание.</returns>
    [Public]
    public static IMeeting GetContextMeeting()
    {
      if (CallContext.CalledFrom(Meetings.Info))
      {
        var meetingId = CallContext.GetCallerEntityId(Meetings.Info);
        return Meetings.GetAll(m => Equals(m.Id, meetingId)).SingleOrDefault();
      }
      return null;
    }
    
    /// <summary>
    /// Найти или создать повестку совещания.
    /// </summary>
    /// <returns>Повестки совещания.</returns>
    [Remote]
    public List<IAgenda> GetOrCreateAgenda()
    {
      var agendaList = Agendas.GetAll(d => Equals(d.Meeting, _obj)).ToList();
      if (!agendaList.Any() && Docflow.PublicFunctions.Module.Remote.IsModuleAvailableForCurrentUserByLicense(Sungero.Meetings.Constants.Module.MeetingsUIGuid))
      {
        var agenda = Agendas.Create();
        agenda.Meeting = _obj;
        agendaList.Add(agenda);
      }
      return agendaList;
    }

    /// <summary>
    /// Найти или создать протокол совещания.
    /// </summary>
    /// <returns>Протоколы совещания.</returns>
    [Remote]
    public List<IMinutes> GetOrCreateMinutes()
    {
      var minutesList = Minuteses.GetAll(d => Equals(d.Meeting, _obj)).ToList();
      if (!minutesList.Any())
      {
        var minutes = Minuteses.Create();
        minutes.Meeting = _obj;
        minutesList.Add(minutes);
      }
      return minutesList;
    }

    /// <summary>
    /// Список документов по совещанию.
    /// </summary>
    /// <param name="relationName">Наименование типа связи, пустая строка - без ограничения по типу связи.</param>
    /// <returns>Документы совещания.</returns>
    [Remote]
    public List<IElectronicDocument> GetMeetingDocuments(string relationName)
    {
      var documentList = new List<IElectronicDocument>();
      var documents = new List<IElectronicDocument>();
      documents.AddRange(Agendas.GetAll(a => Equals(a.Meeting, _obj)).ToList());
      documents.AddRange(Sungero.Meetings.Minuteses.GetAll(m => Equals(m.Meeting, _obj)).ToList());
      
      if (relationName == string.Empty)
      {
        foreach (var document in documents)
          documentList.AddRange(document.Relations.GetRelated().Union(document.Relations.GetRelatedFrom()).Distinct().ToList());
      }
      else
      {
        foreach (var document in documents)
          documentList.AddRange(document.Relations.GetRelated(relationName).Union(document.Relations.GetRelatedFrom(relationName)).Distinct().ToList());
      }
      
      documentList.AddRange(documents);
      documentList = documentList.Distinct().ToList();
      return documentList;
    }
    
    /// <summary>
    /// Выдать права на документ участникам совещания.
    /// </summary>
    /// <param name="meeting">Совещание.</param>
    /// <param name="document">Документ.</param>
    [Public]
    public static void SetAccessRightsOnDocument(IMeeting meeting, IElectronicDocument document)
    {
      if (meeting == null)
        return;
      
      if (document.AccessRights.StrictMode == AccessRightsStrictMode.Enhanced)
        return;
      
      var secretary = meeting.Secretary;
      if (secretary != null && !document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, secretary))
        document.AccessRights.Grant(secretary, DefaultAccessRightsTypes.Change);
      
      var president = meeting.President;
      if (president != null && !document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, president))
        document.AccessRights.Grant(president, DefaultAccessRightsTypes.Change);

      var members = meeting.Members.Select(m => m.Member).ToList();
      foreach (var member in members)
        if (!document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Read, member))
          document.AccessRights.Grant(member, DefaultAccessRightsTypes.Read);
    }
    
    /// <summary>
    /// Построить модель контроля состояния совещания.
    /// </summary>
    /// <returns>Контрол состояния.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStateView()
    {
      var stateView = StateView.Create();
      stateView.AddDefaultLabel(Docflow.OfficialDocuments.Resources.StateViewDefault);
      
      var minutes = Minuteses.GetAll(d => Equals(d.Meeting, _obj));
      var documentsGroupGuid = Docflow.PublicConstants.Module.TaskMainGroup.ActionItemExecutionTask;
      var actionItems = RecordManagement.ActionItemExecutionTasks.GetAll()
        .Where(t => t.AttachmentDetails.Any(g => g.GroupId == documentsGroupGuid && minutes.Any(m => m.Id == g.AttachmentId)))
        .OrderBy(task => task.Created);
      
      var statusesCache = new Dictionary<Enumeration?, string>();
      
      foreach (var actionItem in actionItems)
      {
        if (stateView.Blocks.Any(b => b.HasEntity(actionItem)))
          continue;
        
        var stateViewModel = Sungero.RecordManagement.Structures.ActionItemExecutionTask.StateViewModel.Create();
        stateViewModel.StatusesCache = statusesCache;
        var blocks = RecordManagement.PublicFunctions.ActionItemExecutionTask.GetActionItemExecutionTaskStateView(actionItem, actionItem, stateViewModel, null, true, false).Blocks;
        statusesCache = stateViewModel.StatusesCache;
        
        // Убираем первый блок с текстовой информацией по поручению.
        foreach (var block in blocks.Skip(1))
          stateView.AddBlock(block);
      }
      return stateView;
    }
    
    /// <summary>
    /// Получить нумерованный список участников совещания.
    /// </summary>
    /// <param name="onlyMembers">Признак отображения только списка участников.</param>
    /// <param name="withJobTitle">Признак отображения должности участников.</param>
    /// <returns>Нумерованный список участников совещания.</returns>
    [Remote, Public]
    public virtual string GetMeetingMembersString(bool onlyMembers, bool withJobTitle)
    {
      var members = _obj.Members.Select(x => x.Member).ToList();
      var employees = Company.PublicFunctions.Module.Remote.GetEmployeesFromRecipientsRemote(members);
      if (_obj.Secretary != null)
        employees.Insert(0, _obj.Secretary);
      if (_obj.President != null)
        employees.Insert(0, _obj.President);
      
      if (onlyMembers)
        employees = employees.Where(x => !Equals(x, _obj.President))
          .Where(x => !Equals(x, _obj.Secretary))
          .ToList();
      
      return Company.PublicFunctions.Employee.Remote.GetEmployeesNumberedList(employees, withJobTitle);
    }
    
    /// <summary>
    /// Получить имя совещания в обход прав доступа.
    /// </summary>
    /// <param name="id">Ид совещания.</param>
    /// <returns>Имя совещания с датой совещания.</returns>
    [Remote]
    public static string GetMeetingNameWithDateIgnoreAccessRights(int id)
    {
      var name = string.Empty;
      AccessRights.AllowRead(
        () =>
        {
          var meeting = Meetings.GetAll(m => Equals(m.Id, id)).FirstOrDefault();
          if (meeting.DateTime != null)
            name += Docflow.OfficialDocuments.Resources.DateFrom + meeting.DateTime.Value.ToString("d");
          name += GetMeetingNameIgnoreAccessRights(id);
        });
      return name;
    }
    
    /// <summary>
    /// Получить имя совещания в обход прав доступа.
    /// </summary>
    /// <param name="id">Ид совещания.</param>
    /// <returns>Имя совещания.</returns>
    [Remote]
    public static string GetMeetingNameIgnoreAccessRights(int id)
    {
      var name = string.Empty;
      AccessRights.AllowRead(
        () =>
        {
          var meeting = Meetings.GetAll(m => Equals(m.Id, id)).FirstOrDefault();
          if (!string.IsNullOrWhiteSpace(meeting.Name))
            name += string.Format(" {0} \"{1}\"", Agendas.Resources.For, meeting.Name);
        });
      return name;
    }
    
    /// <summary>
    /// Получить список поручений по совещанию.
    /// </summary>
    /// <returns>Список поручений.</returns>
    [Remote]
    public List<RecordManagement.IActionItemExecutionTask> GetActionItemsByMeeting()
    {
      var minutes = Minuteses.GetAll(d => Equals(d.Meeting, _obj));
      var documentsGroupGuid = Docflow.PublicConstants.Module.TaskMainGroup.ActionItemExecutionTask;
      return RecordManagement.ActionItemExecutionTasks.GetAll()
        .Where(t => t.AttachmentDetails.Any(g => g.GroupId == documentsGroupGuid && minutes.Any(m => m.Id == g.AttachmentId)))
        .ToList();
    }
    
    /// <summary>
    /// Добавить получателей в участников совещания, исключая дублирующие записи.
    /// </summary>
    /// <param name="recipient">Реципиент.</param>
    [Public, Remote]
    public void SetRecipientToMembers(IRecipient recipient)
    {
      var employees = Company.PublicFunctions.Module.Remote.GetEmployeesFromRecipientsRemote(new List<IRecipient> { recipient });
      
      var currentEmployees = _obj.Members.Where(x => Sungero.Company.Employees.Is(x.Member))
        .Select(x => Sungero.Company.Employees.As(x.Member));
      employees = employees.Except(currentEmployees)
        .Where(x => !Equals(x, _obj.Secretary))
        .Where(x => !Equals(x, _obj.President))
        .ToList();
      
      foreach (var employee in employees)
        _obj.Members.AddNew().Member = employee;
    }
  }
}