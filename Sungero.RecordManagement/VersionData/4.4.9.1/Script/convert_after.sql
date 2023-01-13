update sungero_wf_task
set receiveoncompl_recman_sungero = 'Assignment'
where discriminator = '2D53959B-2CEE-41F7-83C2-98AE1DBBD538'
  and receiveoncompl_recman_sungero = ''
  or receiveoncompl_recman_sungero is null