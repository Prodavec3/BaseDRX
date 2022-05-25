using System;

namespace Sungero.Shell.Constants
{
  public static class Module
  {

    /// <summary>
    /// Гуид входящего письма.
    /// </summary>
    public const string IncomingLetterGuid = "8dd00491-8fd0-4a7a-9cf3-8b6dc2e6455d";
        
    #region Виджеты
    
    #region Идентификаторы значений серий
    public const string OverduedAssignments = "Overdued";
    
    public const string NotOverduedAssignments = "NotOverdued";
   
    public static class TodayAssignments
    {
      public const string CompletedToday = "CompletedToday";
      public const string DeadlineToday = "DeadlineToday";
      public const string OverdueToday = "OverdueToday";
      
      public const string DeadlineTomorrow = "DeadlineTomorrow";
      public const string AfterTomorrow = "AfterTomorrow";
      public const string EndOfWeek = "EndOfWeek";
      public const string NextEndOfWeek = "NextEndOfWeek";
      public const string EndOfMonth = "EndOfMonth";
    }
    #endregion
    
    #endregion
    
    // Цвета графиков.
    public static class Colors
    {
      public const string Red = "#FF5000";
      
      public const string Orange = "#E89314";
      
      public const string Yellow = "#FCC72F";
      
      public const string LightYellowGreen = "#BAC238";
      
      public const string YellowGreen = "#7DAB3A";
      
      public const string Green = "#4FAA37";
    }
    
    // Режимы фильтрации заданий.
    public static class FilterAssignmentsMode
    {
      public const string Default = "Default";
      
      public const string Created = "Created";
      
      public const string Modified = "Modified";
      
      public const string Completed = "Completed";
    }
    
  }
}