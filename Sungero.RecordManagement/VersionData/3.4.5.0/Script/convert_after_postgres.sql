-- 3.3.40.0:
DO $$

declare acqParticipantDiscriminator uuid = '481ce37a-03d3-43b6-ac5b-3d8bcf9d3f9b';
declare status varchar(6) = 'Active';
declare acqParticipantNewId int;
declare secureObject int = null;
declare name citext = null;
declare newAcqParticipantsCount int;

declare acqTaskEmplsDiscriminator uuid = '955eb1eb-e18a-48d9-b56d-164a7366bf4c';
declare acqTaskEmplsNewId int;
declare newAcqTaskEmplsCount int;

begin

if exists(select 1 from INFORMATION_SCHEMA.TABLES where table_name = 'sungero_recman_acqparticipant')
then

if not exists(select 1 from sungero_recman_acqparticipant)
then

----------- Заполнение таблицы sungero_recman_acqparticipant ------------------------

if exists(select * from information_schema.tables where table_name = 'temp_taskstable')
then
  drop table temp_taskstable;
end if;

create table temp_taskstable
(
  Id serial,
  TaskId int
);

insert into temp_taskstable (TaskId)
select 
  distinct Task
from Sungero_RecMan_Acquainters
order by Task;

-- Подсчет количества новых элементов очереди.
select COUNT(TaskId) into newAcqParticipantsCount from temp_taskstable;

-- Выделение id в таблице sungero_recman_acqparticipant.
acqParticipantNewId := (select sungero_system_GetNewId('sungero_recman_acqparticipant', newAcqParticipantsCount));

insert into Sungero_RecMan_AcqParticipant
(
  Id,
  Discriminator,
  SecureObject,
  Status,
  Name,
  TaskId
)
select
  -- В PostgreSQL данные типа serial всегда начинаются с единицы, поэтому ее нужно вычитать.
  acqParticipantNewId + t.Id - 1,
  AcqParticipantDiscriminator,
  secureObject,
  status,
  name,
  t.TaskId
from temp_taskstable as t;

drop table temp_taskstable;

------------------ Заполнение таблицы sungero_recman_acqtaskempls ------------------------

if exists(select * from information_schema.tables where table_name = 'temp_participantstable')
then
  drop table temp_participantstable;
end if;

create table temp_participantstable
(
  Id serial,
  TaskId int,
  Acquainter int
);

insert into temp_participantstable (TaskId, Acquainter)
select 
  Task, 
  Acquainter
from Sungero_RecMan_Acquainters;

-- Подсчет количества новых элементов очереди.
select COUNT(Acquainter) into newAcqTaskEmplsCount from temp_participantstable;

-- Выделение id в таблице sungero_recman_acqtaskempls.
acqTaskEmplsNewId := (select Sungero_System_GetNewId('sungero_recman_acqtaskempls', newAcqTaskEmplsCount));

insert into Sungero_RecMan_AcqTaskEmpls
(
  Id,
  Discriminator,
  AcqParticipant,
  Employee
)
select
  -- В PostgreSQL данные типа serial всегда начинаются с единицы, поэтому ее нужно вычитать.
  acqTaskEmplsNewId + t.Id - 1,
  acqTaskEmplsDiscriminator,
  tasks.Id,
  t.Acquainter
from 
  temp_participantstable as t
  join Sungero_RecMan_AcqParticipant tasks on tasks.TaskId = t.TaskId;

drop table temp_participantstable;

end if;
end if;
end $$;


-- 3.3.48.0:
DO $$
begin

if exists(select 1 from INFORMATION_SCHEMA.TABLES where table_name = 'sungero_docflow_params')
then

update
  sungero_docflow_params
set
  value = '2000'
where
  key = 'AcquaintanceTaskPerformersLimit'
  and value = '1000';

end if;
end $$