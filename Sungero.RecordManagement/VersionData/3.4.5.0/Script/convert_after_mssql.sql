-- 3.3.40.0:
if exists(select 1 from INFORMATION_SCHEMA.TABLES where table_name = 'Sungero_RecMan_AcqParticipant')
begin

if not exists(select 1 from Sungero_RecMan_AcqParticipant)
 begin

----------- Заполнение таблицы Sungero_RecMan_AcqParticipant ------------------------
declare @temp_TasksTable table
(
  Id int identity(0,1),
  TaskId int
)

insert into @temp_TasksTable
select
  distinct Task
from [Sungero_RecMan_Acquainters]

declare @acqParticipantDiscriminator varchar(36) = '481ce37a-03d3-43b6-ac5b-3d8bcf9d3f9b'
declare @status varchar(6) = 'Active'
declare @acqParticipantNewId int
declare @secureObject int = null
declare @name nvarchar = null
declare @newAcqParticipantsCount int

-- Подсчет количества новых элементов очереди.
select @newAcqParticipantsCount = 
  COUNT(TaskId)
from 
  @temp_TasksTable

-- Выделение id в таблице Sungero_RecMan_AcqParticipant.
exec Sungero_System_GetNewId 'Sungero_RecMan_AcqParticipant', @acqParticipantNewId output, @newAcqParticipantsCount

insert into Sungero_RecMan_AcqParticipant
(
  [Id],
  [Discriminator],
  [SecureObject],
  [Status],
  [Name],
  [TaskId]
)
select
  @acqParticipantNewId + t.Id,
  @AcqParticipantDiscriminator,
  @secureObject,
  @status,
  @name,
  t.TaskId
from @temp_TasksTable as t

------------------ Заполнение таблицы Sungero_RecMan_AcqTaskEmpls ------------------------

declare @temp_ParticipantsTable table
(
  Id int identity(0,1),
  TaskId int,
  Acquainter int
)

insert into @temp_ParticipantsTable
select
  Task,
  Acquainter
from [Sungero_RecMan_Acquainters]

declare @acqTaskEmplsDiscriminator varchar(36) = '955EB1EB-E18A-48D9-B56D-164A7366BF4C'
declare @acqTaskEmplsNewId int
declare @newAcqTaskEmplsCount int

-- Подсчет количества новых элементов очереди.
select @newAcqTaskEmplsCount =
  COUNT(Acquainter)
from 
  @temp_ParticipantsTable

-- Выделение id в таблице Sungero_RecMan_AcqTaskEmpls.
exec Sungero_System_GetNewId 'Sungero_RecMan_AcqTaskEmpls', @acqTaskEmplsNewId output, @newAcqTaskEmplsCount

insert into Sungero_RecMan_AcqTaskEmpls
(
  [Id],
  [Discriminator],
  [AcqParticipant],
  [Employee]
)
select
  @acqTaskEmplsNewId + t.Id,
  @acqTaskEmplsDiscriminator,
  tasks.Id,
  t.Acquainter
from 
  @temp_ParticipantsTable as t
  join Sungero_RecMan_AcqParticipant tasks on tasks.TaskId = t.TaskId
  
end
end


-- 3.3.48.0:
if exists(select 1 from INFORMATION_SCHEMA.TABLES where table_name = 'Sungero_Docflow_Params')
begin

update
  Sungero_Docflow_Params
set
  [value] = '2000'
where
  [key] = 'AcquaintanceTaskPerformersLimit'
  and [value] = '1000'

end