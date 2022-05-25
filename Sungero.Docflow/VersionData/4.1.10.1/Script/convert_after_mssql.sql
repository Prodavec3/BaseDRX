--Обновление задачи на согласование по регламенту.

-- Установить свойство "Несколько адресатов" в задаче на согласование по регламенту в False.
update t
set ApprIsManyAddr_Docflow_Sungero = 'false'
from Sungero_WF_Task t 
where ApprIsManyAddr_Docflow_Sungero is null
  and Discriminator = '100950d0-03d2-44f0-9e31-f9c8dfdf3829'

if exists(select 1 from INFORMATION_SCHEMA.TABLES where table_name = 'Sungero_Docflow_TAprAddressees')
begin

declare @temp_TasksTable table
(
  Id int identity(1,1),
  TaskId int,
  AddresseeId int
)

-- Используется дискриминатор задачи на согласование по регламенту (100950d0-03d2-44f0-9e31-f9c8dfdf3829).
insert into @temp_TasksTable
select distinct
  task.Id,
  task.Addressee_Docflow_Sungero
from Sungero_WF_Task as task
  left join Sungero_Docflow_TAprAddressees as addr on task.Id = addr.Task
where task.Discriminator = '100950d0-03d2-44f0-9e31-f9c8dfdf3829'
  and task.Addressee_Docflow_Sungero is not null
  and addr.Task is null

-- Дискриминатор свойства-коллекции адресатов задачи на согласование по регламенту (342199D9-3901-451F-B03A-2255B9DB6377).
declare @addresseesDiscriminator varchar(36) = '342199D9-3901-451F-B03A-2255B9DB6377'

declare @addresseesNewId int
declare @tasksToConvertCount int
declare @TAprAddresseesTableLastId int

-- Подсчет количества новых элементов.
select @tasksToConvertCount = 
  COUNT(TaskId)
from 
  @temp_TasksTable

-- Получение последнего ИД в таблице для свойства-коллекции адресатов задачи на согласование по регламенту.
select @TAprAddresseesTableLastId = LastId
from Sungero_System_Ids
where TableName = 'Sungero_Docflow_TAprAddressees'

-- Резервирование id в таблице.
exec Sungero_System_GetNewId 'Sungero_Docflow_TAprAddressees', @addresseesNewId output, @tasksToConvertCount

insert into Sungero_Docflow_TAprAddressees
(
  [Id],
  [Discriminator],
  [Task],
  [Addressee]
)
select
  @TAprAddresseesTableLastId + t.Id,
  @addresseesDiscriminator,
  t.TaskId,
  t.AddresseeId
from @temp_TasksTable as t

end


--Обновление задания на согласование руководителем.

if exists(select 1 from INFORMATION_SCHEMA.TABLES where table_name = 'Sungero_Docflow_AAprManAddr')
begin

declare @temp_ManagerAssignmentsTable table
(
  Id int identity(1,1),
  AssignmentId int,
  AddresseeId int
)

-- Используется дискриминатор задания на согласование руководителем (bbb08f45-60c1-4496-9ff6-b32caed44215).
insert into @temp_ManagerAssignmentsTable
select distinct
  asg.Id,
  asg.AddresseeMan_Docflow_Sungero
from Sungero_WF_Assignment as asg
  left join Sungero_Docflow_AAprManAddr as addr on asg.Id = addr.Assignment
where asg.Discriminator = 'bbb08f45-60c1-4496-9ff6-b32caed44215'
  and asg.AddresseeMan_Docflow_Sungero is not null
  and addr.Assignment is null

-- Дискриминатор свойства-коллекции адресатов задания на согласование руководителем (5DB88398-C7FD-48DF-95BC-F70F00A67ADE).
declare @addresseesManagerAsgDiscriminator varchar(36) = '5DB88398-C7FD-48DF-95BC-F70F00A67ADE'

declare @addresseesManagerAsgNewId int
declare @ManagerAssignmentsToConvertCount int
declare @AAprManAddresseesTableLastId int

-- Подсчет количества новых элементов.
select @ManagerAssignmentsToConvertCount = 
  COUNT(AssignmentId)
from 
  @temp_ManagerAssignmentsTable

-- Получение последнего ИД в таблице для свойства-коллекции адресатов задания на согласование руководителем.
select @AAprManAddresseesTableLastId = LastId
from Sungero_System_Ids
where TableName = 'Sungero_Docflow_AAprManAddr'

-- Резервирование id в таблице.
exec Sungero_System_GetNewId 'Sungero_Docflow_AAprManAddr', @addresseesManagerAsgNewId output, @ManagerAssignmentsToConvertCount

insert into Sungero_Docflow_AAprManAddr
(
  [Id],
  [Discriminator],
  [Assignment],
  [Addressee]
)
select
  @AAprManAddresseesTableLastId + t.Id,
  @addresseesManagerAsgDiscriminator,
  t.AssignmentId,
  t.AddresseeId
from @temp_ManagerAssignmentsTable as t

end


--Обновление задания на доработку.

if exists(select 1 from INFORMATION_SCHEMA.TABLES where table_name = 'Sungero_Docflow_AAprRewAdrs')
begin

declare @temp_ReworkAssignmentsTable table
(
  Id int identity(1,1),
  AssignmentId int,
  AddresseeId int
)

-- Используется дискриминатор задания на доработку (040862cd-a46f-4366-b068-e659c7acaea6).
insert into @temp_ReworkAssignmentsTable
select distinct
  asg.Id,
  asg.AddresseeRwk_Docflow_Sungero
from Sungero_WF_Assignment as asg
  left join Sungero_Docflow_AAprRewAdrs as addr on asg.Id = addr.Assignment
where asg.Discriminator = '040862cd-a46f-4366-b068-e659c7acaea6'
  and asg.AddresseeRwk_Docflow_Sungero is not null
  and addr.Assignment is null

-- Дискриминатор свойства-коллекции адресатов задания на доработку (83fd57d1-e450-48bd-94d1-09926bcd2721).
declare @addresseesReworkAsgDiscriminator varchar(36) = '83fd57d1-e450-48bd-94d1-09926bcd2721'

declare @addresseesReworkAsgNewId int
declare @ReworkAssignmentsToConvertCount int
declare @AAprRewAddresseesTableLastId int

-- Подсчет количества новых элементов.
select @ReworkAssignmentsToConvertCount = 
  COUNT(AssignmentId)
from 
  @temp_ReworkAssignmentsTable

-- Получение последнего ИД в таблице для свойства-коллекции адресатов задания на доработку.
select @AAprRewAddresseesTableLastId = LastId
from Sungero_System_Ids
where TableName = 'Sungero_Docflow_AAprRewAdrs'

-- Резервирование id в таблице.
exec Sungero_System_GetNewId 'Sungero_Docflow_AAprRewAdrs', @addresseesReworkAsgNewId output, @ReworkAssignmentsToConvertCount

insert into Sungero_Docflow_AAprRewAdrs
(
  [Id],
  [Discriminator],
  [Assignment],
  [Addressee]
)
select
  @AAprRewAddresseesTableLastId + t.Id,
  @addresseesReworkAsgDiscriminator,
  t.AssignmentId,
  t.AddresseeId
from @temp_ReworkAssignmentsTable as t

end