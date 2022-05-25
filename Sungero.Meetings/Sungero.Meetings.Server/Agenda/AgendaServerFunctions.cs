using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Meetings.Agenda;

namespace Sungero.Meetings.Server
{
  partial class AgendaFunctions
  {
    /// <summary>
    /// Создать повестку.
    /// </summary>
    /// <returns>Повестка.</returns>
    [Remote, Public]
    public static IAgenda CreateAgenda()
    {
      return Agendas.Create();
    }
    
    /// <summary>
    /// Получить председателя совещания.
    /// </summary>
    /// <param name="agenda">Повестка.</param>
    /// <returns>Председатель совещания.</returns>
    [Converter("GetMeetingPresident")]
    public static Company.IEmployee GetMeetingPresident(IAgenda agenda)
    {
      return agenda.Meeting == null ? null : agenda.Meeting.President;
    }
    
    /// <summary>
    /// Получить секретаря совещания.
    /// </summary>
    /// <param name="agenda">Повестка.</param>
    /// <returns>Секретарь совещания.</returns>
    [Converter("GetMeetingSecretary")]
    public static Company.IEmployee GetMeetingSecretary(IAgenda agenda)
    {
      return agenda.Meeting == null ? null : agenda.Meeting.Secretary;
    }
    
    /// <summary>
    /// Получить наименование совещания.
    /// </summary>
    /// <param name="agenda">Повестка.</param>
    /// <returns>Наименование совещания.</returns>
    [Converter("GetMeetingName")]
    public static string GetMeetingName(IAgenda agenda)
    {
      return agenda.Meeting == null ? null : agenda.Meeting.Name;
    }
    
    /// <summary>
    /// Получить список участников совещания.
    /// </summary>
    /// <param name="agenda">Повестка.</param>
    /// <returns>Список участников совещания.</returns>
    [Converter("GetMeetingMembers")]
    public static string GetMeetingMembers(IAgenda agenda)
    {
      return agenda.Meeting == null ? null : PublicFunctions.Meeting.Remote.GetMeetingMembersString(agenda.Meeting, true, false);
    }
    
    /// <summary>
    /// Получить список участников совещания с должностями.
    /// </summary>
    /// <param name="agenda">Повестка.</param>
    /// <returns>Список участников совещания с должностями.</returns>
    [Converter("GetMeetingMembersWithJobTitle")]
    public static string GetMeetingMembersWithJobTitle(IAgenda agenda)
    {
      return agenda.Meeting == null ? null : PublicFunctions.Meeting.Remote.GetMeetingMembersString(agenda.Meeting, true, true);
    }
    
    /// <summary>
    /// Получить дату проведения совещания.
    /// </summary>
    /// <param name="agenda">Повестка.</param>
    /// <returns>Дата проведения совещания.</returns>
    [Converter("GetMeetingDate")]
    public static DateTime? GetMeetingDate(IAgenda agenda)
    {
      return agenda.Meeting == null ? null : agenda.Meeting.DateTime;
    }
    
    /// <summary>
    /// Получить время проведения совещания.
    /// </summary>
    /// <param name="agenda">Повестка.</param>
    /// <returns>Время проведения совещания.</returns>
    [Converter("GetMeetingTime")]
    public static string GetMeetingTime(IAgenda agenda)
    {
      return agenda.Meeting == null ? null : Functions.Meeting.GetMeetingTimeAsString(agenda.Meeting);
    }
    
    /// <summary>
    /// Получить место проведения совещания.
    /// </summary>
    /// <param name="agenda">Повестка.</param>
    /// <returns>Место проведения совещания.</returns>
    [Converter("GetMeetingLocation")]
    public static string GetMeetingLocation(IAgenda agenda)
    {
      return agenda.Meeting == null ? null : agenda.Meeting.Location;
    }
  }
}