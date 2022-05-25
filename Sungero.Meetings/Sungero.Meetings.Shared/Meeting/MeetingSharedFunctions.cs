using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Meetings.Meeting;

namespace Sungero.Meetings.Shared
{
  partial class MeetingFunctions
  {
    /// <summary>
    /// Получить имя совещания в формате: " от [дата совещания] по [тема совещания]".
    /// </summary>
    /// <returns>Имя совещания с датой.</returns>
    public string GetMeetingNameWithDate()
    {
      if (!_obj.AccessRights.CanRead())
        return Functions.Meeting.Remote.GetMeetingNameWithDateIgnoreAccessRights(_obj.Id);
      else
      {
        var name = string.Empty;
        if (_obj.DateTime != null)
          name += Docflow.OfficialDocuments.Resources.DateFrom + _obj.DateTime.Value.ToString("d");
        name += this.GetMeetingName();
        return name;
      }
    }
    
    /// <summary>
    /// Получить имя совещания в формате: " по [тема совещания]".
    /// </summary>
    /// <returns>Имя совещания.</returns>
    public string GetMeetingName()
    {
      if (!_obj.AccessRights.CanRead())
        return Functions.Meeting.Remote.GetMeetingNameIgnoreAccessRights(_obj.Id);
      else
      {
        using (TenantInfo.Culture.SwitchTo())
        {
          var name = string.Empty;
          if (!string.IsNullOrWhiteSpace(_obj.Name))
            name += string.Format(" {0} \"{1}\"", Agendas.Resources.For, _obj.Name);
          return name;
        }
      }
    }
    
    /// <summary>
    /// Получить время совещания.
    /// </summary>
    /// <returns>Временной интервал совещания в виде строки.</returns>
    [Public]
    public virtual string GetMeetingTimeAsString()
    {
      return string.Format("{0} – {1}", _obj.DateTime.Value.ToShortTimeString(), _obj.DateTime.Value.AddHours(_obj.Duration.Value).ToShortTimeString());
    }
  }
}