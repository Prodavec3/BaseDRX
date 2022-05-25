using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Meetings.Meeting;

namespace Sungero.Meetings.Client
{
  partial class MeetingActions
  {
    public virtual void AddMember(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var membersCount = _obj.Members.Count;
      var recipients = Company.PublicFunctions.Module.GetAllActiveNoSystemGroups()
        .Where(x => !Company.BusinessUnits.Is(x));
      
      var member = recipients.ShowSelect(Meetings.Resources.SelectDepartmentOrRole);
      Sungero.Meetings.PublicFunctions.Meeting.Remote.SetRecipientToMembers(_obj, member);
      
      // Если были выбраны новые участники, то показать всплывающее сообщение с результатами выбора.
      if (member != null)
        if (membersCount == _obj.Members.Count)
          Sungero.Core.Dialogs.NotifyMessage(Sungero.Meetings.Meetings.Resources.MembersAlreadyEntered);
        else Sungero.Core.Dialogs.NotifyMessage(Sungero.Meetings.Meetings.Resources.NewMembersAdded);
    }

    public virtual bool CanAddMember(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate();
    }

    public virtual void AddToCalendar(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var meeting = MeetingsCalendar.CreateMeeting();
      meeting.Summary = _obj.Name;
      meeting.Location = _obj.Location;
      
      var startTime = Calendar.Now;
      if (_obj.DateTime.HasValue)
        startTime = _obj.DateTime.Value;
      meeting.StartTime = startTime;
      
      var endTime = startTime.AddHours(1);
      if (_obj.Duration.HasValue)
        endTime = startTime.AddHours(_obj.Duration.Value);
      meeting.EndTime = endTime;
      
      string presidentName = string.Empty;
      if (_obj.President != null)
      {
        presidentName = Sungero.Parties.PublicFunctions.Module.GetSurnameAndInitialsInTenantCulture(_obj.President.Person.FirstName,
                                                                                                    _obj.President.Person.MiddleName,
                                                                                                    _obj.President.Person.LastName);
        presidentName = Sungero.Meetings.Meetings.Resources.HtmlChairpersonTemplateFormat(presidentName);
      }
        
      string secretaryName = string.Empty;
      if (_obj.Secretary != null)
      {
        secretaryName = Sungero.Parties.PublicFunctions.Module.GetSurnameAndInitialsInTenantCulture(_obj.Secretary.Person.FirstName,
                                                                                                    _obj.Secretary.Person.MiddleName,
                                                                                                    _obj.Secretary.Person.LastName);
        secretaryName = Sungero.Meetings.Meetings.Resources.HtmlSecretaryTemplateFormat(secretaryName);
        if (_obj.President == null)
          secretaryName = string.Format("<br>{0}", secretaryName);
      }
        
      string meetingNote = string.Empty;
      if (!string.IsNullOrWhiteSpace(_obj.Note))
        meetingNote = string.Format("<br><br>{0}", _obj.Note);
      
      var meetingDocuments = Functions.Meeting.Remote.GetMeetingDocuments(_obj, string.Empty);
      var documentsList = new List<string>();
      string documentsListString = string.Empty;
      if (meetingDocuments.Any())
      {
        foreach (var document in meetingDocuments)
          documentsList.Add(string.Format("<a href=\"{0}\">{1}</a>", Hyperlinks.Get(document), document.Name));
        documentsListString = Sungero.Meetings.Meetings.Resources.HtmlMeetingDocumentsTemplateFormat(string.Join("<br>", documentsList));
      }
      
      meeting.HtmlDescription = Sungero.Meetings.Meetings.Resources.HtmlDescriptionMeetingTemplateFormat(Hyperlinks.Get(_obj),
                                                                                                         _obj.DisplayName,
                                                                                                         presidentName,
                                                                                                         secretaryName,
                                                                                                         meetingNote,
                                                                                                         documentsListString);
      MeetingsCalendar.ShowMeeting(meeting);
    }

    public virtual bool CanAddToCalendar(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

    public virtual void OpenActionItems(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Meeting.Remote.GetActionItemsByMeeting(_obj).Show();
    }

    public virtual bool CanOpenActionItems(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

    public virtual void MeetingDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var documents = Functions.Meeting.Remote.GetMeetingDocuments(_obj, string.Empty);
      documents.Show(e.Action.LocalizedName);
    }

    public virtual bool CanMeetingDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }
    
    public virtual void OpenActionItemExecutionReport(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var actionItemsArePresent = RecordManagement.PublicFunctions.Module.Remote.ActionItemCompletionDataIsPresent(_obj, null);
      
      if (!actionItemsArePresent)
      {
        Dialogs.NotifyMessage(RecordManagement.Reports.Resources.ActionItemsExecutionReport.NoAnyActionItemsForMeeting);
        return;
      }
      else
      {
        var actionItemExecutionReport = RecordManagement.Reports.GetActionItemsExecutionReport();
        actionItemExecutionReport.Meeting = _obj;
        actionItemExecutionReport.Open();
      }
    }

    public virtual bool CanOpenActionItemExecutionReport(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

    public virtual void CreateOrShowMinutes(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var minutes = Functions.Meeting.Remote.GetOrCreateMinutes(_obj);
      if (minutes.Count == 1)
        minutes.Single().Show();
      else
        minutes.Show();
    }

    public virtual bool CanCreateOrShowMinutes(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

    public virtual void CreateOrShowAgenda(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var agendas = Functions.Meeting.Remote.GetOrCreateAgenda(_obj);
      if (agendas.Count == 1)
        agendas.Single().Show();
      else if (!agendas.Any() && !Docflow.PublicFunctions.Module.Remote.IsModuleAvailableForCurrentUserByLicense(Sungero.Meetings.Constants.Module.MeetingsUIGuid))
        e.AddWarning(Sungero.Meetings.Meetings.Resources.NoLicenceToCreateAgenda);
      else
        agendas.Show();
    }

    public virtual bool CanCreateOrShowAgenda(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

  }

}