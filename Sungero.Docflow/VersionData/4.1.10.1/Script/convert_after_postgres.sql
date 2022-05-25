DO $$

-- Дискриминатор свойства-коллекции адресатов задачи на согласование по регламенту (342199D9-3901-451F-B03A-2255B9DB6377).
declare addresseesDiscriminator uuid = '342199D9-3901-451F-B03A-2255B9DB6377';

-- Дискриминатор свойства-коллекции адресатов задания на согласование руководителем (5DB88398-C7FD-48DF-95BC-F70F00A67ADE).
declare addresseesManagerAsgDiscriminator uuid = '5DB88398-C7FD-48DF-95BC-F70F00A67ADE';

-- Дискриминатор свойства-коллекции адресатов задания на доработку (83fd57d1-e450-48bd-94d1-09926bcd2721).
declare addresseesReworkAsgDiscriminator uuid = '83fd57d1-e450-48bd-94d1-09926bcd2721';

declare addresseesNewId int;
declare tasksToConvertCount int;
declare TAprAddresseesTableLastId int;

declare addresseesManagerAsgNewId int;
declare managerAssignmentsToConvertCount int;
declare aAprManAddresseesTableLastId int;

declare addresseesReworkAsgNewId int;
declare reworkAssignmentsToConvertCount int;
declare aAprRewAddresseesTableLastId int;

begin

--Обновление задачи на согласование по регламенту.

-- Установить свойство "Несколько адресатов" в задаче на согласование по регламенту в False.
update sungero_wf_task
set apprismanyaddr_docflow_sungero = 'false'
where apprismanyaddr_docflow_sungero is null
  and discriminator = '100950d0-03d2-44f0-9e31-f9c8dfdf3829';

if exists(select 1 from information_schema.tables where table_name = 'sungero_docflow_tapraddressees')
then

if exists(select * from information_schema.tables where table_name = 'temp_taskstable')
then
  drop table temp_taskstable;
end if;

create table temp_taskstable
(
  id serial,
  taskid int,
  addresseeid int 
);

-- Используется дискриминатор задачи на согласование по регламенту (100950d0-03d2-44f0-9e31-f9c8dfdf3829).
insert into temp_taskstable (taskid, addresseeid)
select distinct
  task.id, 
  task.addressee_docflow_sungero
from sungero_wf_task as task
  left join sungero_docflow_tapraddressees as addr on task.id = addr.task
where task.discriminator = '100950d0-03d2-44f0-9e31-f9c8dfdf3829'
  and task.addressee_docflow_sungero is not null
  and addr.task is null
order by task.id;

-- Подсчет количества новых элементов.
select count(taskid) into tasksToConvertCount from temp_taskstable;

-- Получение последнего ИД в таблице для свойства-коллекции адресатов задачи на согласование по регламенту.
TAprAddresseesTableLastId := (select lastid
  from sungero_system_ids
  where tablename = 'sungero_docflow_tapraddressees');

-- Резервирование id в таблице.
addresseesNewId := (select sungero_system_GetNewId('sungero_docflow_tapraddressees', tasksToConvertCount));

insert into sungero_docflow_tapraddressees
(
  id,
  discriminator,
  task,
  addressee
)
select
  TAprAddresseesTableLastId + t.Id,
  addresseesDiscriminator,
  t.taskid,
  t.addresseeid
from temp_taskstable as t;

drop table temp_taskstable;

end if;


-- Обновление задания на согласование руководителем.

if exists(select 1 from information_schema.tables where table_name = 'sungero_docflow_aaprmanaddr')
then

if exists(select * from information_schema.tables where table_name = 'temp_managerassignmentstable')
then
  drop table temp_managerassignmentstable;
end if;

create table temp_managerassignmentstable
(
  id serial,
  assignmentid int,
  addresseeid int 
);

-- Используется дискриминатор задания на согласование руководителем (100950d0-03d2-44f0-9e31-f9c8dfdf3829).
insert into temp_managerassignmentstable (assignmentid, addresseeid)
select distinct
  asg.id, 
  asg.addresseeman_docflow_sungero
from sungero_wf_assignment as asg
  left join sungero_docflow_aaprmanaddr as addr on asg.id = addr.assignment
where asg.discriminator = '100950d0-03d2-44f0-9e31-f9c8dfdf3829'
  and asg.addresseeman_docflow_sungero is not null
  and addr.assignment is null
order by asg.id;

-- Подсчет количества новых элементов.
select count(assignmentid) into managerAssignmentsToConvertCount from temp_managerassignmentstable;

-- Получение последнего ИД в таблице для свойства-коллекции адресатов задания на согласование руководителем.
aAprManAddresseesTableLastId := (select lastid
  from sungero_system_ids
  where tablename = 'sungero_docflow_aaprmanaddr');

-- Резервирование id в таблице.
addresseesManagerAsgNewId := (select sungero_system_GetNewId('sungero_docflow_aaprmanaddr', managerAssignmentsToConvertCount));

insert into sungero_docflow_aaprmanaddr
(
  id,
  discriminator,
  assignment,
  addressee
)
select
  aAprManAddresseesTableLastId + t.Id,
  addresseesManagerAsgDiscriminator,
  t.assignmentid,
  t.addresseeid
from temp_managerassignmentstable as t;

drop table temp_managerassignmentstable;

end if;


-- Обновление задания на доработку.

if exists(select 1 from information_schema.tables where table_name = 'sungero_docflow_aaprrewadrs')
then

if exists(select * from information_schema.tables where table_name = 'temp_reworkassignmentstable')
then
  drop table temp_reworkassignmentstable;
end if;

create table temp_reworkassignmentstable
(
  id serial,
  assignmentid int,
  addresseeid int 
);

-- Используется дискриминатор задания на доработку (040862cd-a46f-4366-b068-e659c7acaea6).
insert into temp_reworkassignmentstable (assignmentid, addresseeid)
select distinct
  asg.id, 
  asg.addresseerwk_docflow_sungero
from sungero_wf_assignment as asg
  left join sungero_docflow_aaprrewadrs as addr on asg.id = addr.assignment
where asg.discriminator = '040862cd-a46f-4366-b068-e659c7acaea6'
  and asg.addresseerwk_docflow_sungero is not null
  and addr.assignment is null
order by asg.id;

-- Подсчет количества новых элементов.
select count(assignmentid) into reworkAssignmentsToConvertCount from temp_reworkassignmentstable;

-- Получение последнего ИД в таблице для свойства-коллекции адресатов задания на доработку.
aAprRewAddresseesTableLastId := (select lastid
  from sungero_system_ids
  where tablename = 'sungero_docflow_aaprrewadrs');

-- Резервирование id в таблице.
addresseesReworkAsgNewId := (select sungero_system_GetNewId('sungero_docflow_aaprrewadrs', reworkAssignmentsToConvertCount));

insert into sungero_docflow_aaprrewadrs
(
  id,
  discriminator,
  assignment,
  addressee
)
select
  aAprRewAddresseesTableLastId + t.Id,
  addresseesReworkAsgDiscriminator,
  t.assignmentid,
  t.addresseeid
from temp_reworkassignmentstable as t;

drop table temp_reworkassignmentstable;

end if;

end $$;