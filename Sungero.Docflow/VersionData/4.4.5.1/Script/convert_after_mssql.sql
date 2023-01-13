IF EXISTS(select 1 from sys.indexes
where object_id = (select object_id from sys.objects where name ='Sungero_WF_Assignment')
and name ='idx_Asg_Discr_Perf_Status_Deadline_Complted_Created')
BEGIN
  DROP INDEX idx_Asg_Discr_Perf_Status_Deadline_Complted_Created ON Sungero_WF_Assignment
END

update sungero_core_job 
set Description = case when Name = 'Документооборот. Рассылка электронных писем о заданиях' 
                         then 'Отправка электронных писем сотрудникам о новых и просроченных заданиях'
                         else 'Send emails about new and overdue assignments to employees'
                  end
where JobId = 'd7044e44-89bc-4fb3-9e44-5491514d9b05'
  and Description in ('Отправка уведомления о заданиях', 'Send notice about assignments');