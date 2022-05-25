using System;
using Sungero.Core;

namespace Sungero.Meetings.Constants
{
  public static class Module
  {
    [Sungero.Core.Public]
    public static readonly Guid MeetingResponsibleRole = Guid.Parse("83D3331C-82E9-44CC-8AF1-46A234DE467D");
    [Sungero.Core.Public]
    public static readonly Guid AgendaKind = Guid.Parse("68B6FB25-7F78-4AB9-AE0C-D8947B99FA24");
    [Sungero.Core.Public]
    public static readonly Guid MinutesKind = Guid.Parse("75D45529-60AE-4D95-9C8F-B1016B766253");
    public static readonly Guid MinutesRegister = Guid.Parse("88DD573B-522C-415B-8965-215845305ACF");
    
    [Public]
    public static readonly Guid MeetingsUIGuid = Guid.Parse("6ea9a047-b597-42eb-8f90-da8c559dd057");
    
  }
}