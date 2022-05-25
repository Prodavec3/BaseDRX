DO $$

-- Дискриминатор свойства-коллекции адресатов задачи на рассмотрение (1390396C-7066-44CC-B667-0802D43305D3).
declare addresseesDiscriminator uuid = '1390396C-7066-44CC-B667-0802D43305D3';

declare addresseesNewId int;
declare tasksToConvertCount int;
declare TRevAddresseesTableLastId int;

begin

if exists(select 1 from INFORMATION_SCHEMA.TABLES where table_name = 'sungero_recman_trevaddressees')
then

if exists(select * from information_schema.tables where table_name = 'temp_taskstable')
then
  drop table temp_taskstable;
end if;

create table temp_taskstable
(
  Id serial,
  TaskId int,
  AddresseeId int 
);

-- Используется дискриминатор задачи на рассмотрение (4ef03457-8b42-4239-a3c5-d4d05e61f0b6).
insert into temp_taskstable (TaskId, AddresseeId)
select distinct
  task.id, 
  task.addressee_recman_sungero
from sungero_wf_task as task
  left join sungero_recman_trevaddressees as addr on task.id = addr.task
where task.discriminator = '4ef03457-8b42-4239-a3c5-d4d05e61f0b6'
  and task.addressee_recman_sungero is not null
  and addr.task is null
order by task.id;

-- Подсчет количества новых элементов.
select COUNT(taskid) into tasksToConvertCount from temp_taskstable;

-- Получение последнего ИД в таблице для свойства-коллекции адресатов задачи на рассмотрение.
TRevAddresseesTableLastId := (select lastid
  from sungero_system_ids
  where tablename = 'Sungero_RecMan_TRevAddressees');

-- Резервирование id в таблице.
addresseesNewId := (select sungero_system_GetNewId('sungero_recman_trevaddressees', tasksToConvertCount));

insert into sungero_recman_trevaddressees
(
  id,
  discriminator,
  task,
  addressee,
  taskcreated
)
select
  TRevAddresseesTableLastId + t.Id,
  addresseesDiscriminator,
  t.taskid,
  t.addresseeid,
  NULL
from temp_taskstable as t;

drop table temp_taskstable;

end if;
end $$;