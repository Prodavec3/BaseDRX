if exists(select 1 from INFORMATION_SCHEMA.TABLES where table_name = 'Sungero_RecMan_TRevAddressees')
begin

declare @temp_TasksTable table
(
  Id int identity(1,1),
  TaskId int,
  AddresseeId int
)

-- Используется дискриминатор задачи на рассмотрение (4ef03457-8b42-4239-a3c5-d4d05e61f0b6).
insert into @temp_TasksTable
select distinct
  task.Id,
  task.Addressee_RecMan_Sungero
from Sungero_WF_Task as task
  left join Sungero_RecMan_TRevAddressees as addr on task.Id = addr.Task
where task.Discriminator = '4ef03457-8b42-4239-a3c5-d4d05e61f0b6'
  and task.Addressee_RecMan_Sungero is not null
  and addr.Task is null

-- Дискриминатор свойства-коллекции адресатов задачи на рассмотрение (1390396C-7066-44CC-B667-0802D43305D3).
declare @addresseesDiscriminator varchar(36) = '1390396C-7066-44CC-B667-0802D43305D3'

declare @addresseesNewId int
declare @tasksToConvertCount int
declare @TRevAddresseesTableLastId int

-- Подсчет количества новых элементов.
select @tasksToConvertCount = 
  COUNT(TaskId)
from 
  @temp_TasksTable

-- Получение последнего ИД в таблице для свойства-коллекции адресатов задачи на рассмотрение.
select @TRevAddresseesTableLastId = LastId
from Sungero_System_Ids
where TableName = 'Sungero_RecMan_TRevAddressees'

-- Резервирование id в таблице.
exec Sungero_System_GetNewId 'Sungero_RecMan_TRevAddressees', @addresseesNewId output, @tasksToConvertCount

insert into Sungero_RecMan_TRevAddressees
(
  [Id],
  [Discriminator],
  [Task],
  [Addressee],
  [TaskCreated]
)
select
  @TRevAddresseesTableLastId + t.Id,
  @addresseesDiscriminator,
  t.TaskId,
  t.AddresseeId,
  NULL
from @temp_TasksTable as t

end