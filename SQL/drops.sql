alter table integrator_integration drop constraint integrator_integration_pk;
alter table integrator_table drop constraint integrator_table_pk;
alter table integrator_table drop constraint integrator_table_integr_fk;
alter table integrator_column drop constraint integrator_column_pk;
alter table integrator_column drop constraint integrator_column_table_fk;

drop table integrator_integration;
drop table integrator_table;
drop table integrator_column;
