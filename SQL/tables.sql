--CRIAR USUARIO E SENHA INTEGRATOR E DAR OS GRANTS NAS TABELAS ABAIXO E NOS OBJETOS DRG
/*
tasy.drg_admissao_atualiza_eup_p,
tasy.drg_admissao_beneficiario_v, 
tasy.drg_admissao_cidsecundario_v, 
tasy.drg_admissao_hospital_v, 
tasy.drg_admissao_internacao_v, 
tasy.drg_admissao_medicoproc_v, 
tasy.drg_admissao_medico_v, 
tasy.drg_admissao_operadora_v, 
tasy.drg_admissao_procedimento_v, 
tasy.drg_admissao_rn_v, 
tasy.drg_alta_analisecritica_v, 
tasy.drg_alta_beneficiario_v, 
tasy.drg_alta_cidsecundario_v, 
tasy.drg_alta_condicaoadquirida_v, 
tasy.drg_alta_cti_v, 
tasy.drg_alta_hospital_v, 
tasy.drg_alta_internacao_v, 
tasy.drg_alta_medicoproc_v, 
tasy.drg_alta_medico_v, 
tasy.drg_alta_operadora_v, 
tasy.drg_alta_procedimento_v, 
tasy.drg_alta_rn_v, 
tasy.drg_custo_paciente_item_v, 
tasy.drg_custo_paciente_v
*/
create table integrator_integration
(
      seq               number(10) not null,
      integration_name  varchar2(255) not null,
      type              varchar2(255) not null,
      base_time         date,
      interval          number(10) not null,
      delay             number(10),
	  paused			varchar2(1),
	  max_tries			number(5),
	  instance_name 	varchar2(255)
      constraint integrator_integration_pk primary key (seq)
);

create table integrator_webservice
(
      seq               number(10) not null,
      url				varchar2(1000),
	  namespace			varchar2(1000),
	  method 			varchar2(1000),
	  seq_integration	number(10) not null,
      constraint integrator_webservice_pk primary key (seq),
	  constraint integrator_webserv_integr_fk foreign key (seq_integration) references integrator_integration(seq)
);

create table integrator_webserv_param
(
      seq               number(10) not null,
      name				varchar2(1000),
	  value				varchar2(1000),
	  cdata				varchar2(1),
	  seq_webservice	number(10) not null,
	  param_order		number(5),
      constraint integrator_webserv_param_pk primary key (seq),
	  constraint integrator_webserv_param_fk foreign key (seq_webservice) references integrator_webservice(seq)
);

create table integrator_segment
(
      seq              number(10) not null,
      tag              varchar2(255) not null,
      seq_parent_segment number(10),
      seq_integration  number(10) not null,
      db_table         varchar2(255),
      db_where         varchar2(255),
	  db_column_date_filter	varchar2(255),
	  main_segment		varchar2(1),
      file_header      varchar2(1000),
      obs           varchar2(1000),
      constraint integrator_segment_pk primary key (seq),
      constraint integrator_segment_integr_fk foreign key (seq_integration) references integrator_integration(seq)
);

create table integrator_field
(
      seq              number(10) not null,
      tag              varchar2(255) not null,
      seq_segment      number(10) not null,
      db_column        varchar2(255),
      field_size       number(10),
      field_order      number(5),
      key_field        varchar2(1),
	  hide        		varchar2(1),
      mask             varchar2(255),
      obs           varchar2(1000),
      constraint integrator_field_pk primary key (seq),
      constraint integrator_field_segment_fk foreign key (seq_segment) references integrator_segment(seq)
);

create table integrator_procedure
(
      seq               number(10) not null,
      owner				varchar2(1000),
	  name				varchar2(1000),
	  seq_webservice	number(10) not null,
      constraint integrator_procedure_pk primary key (seq),
	  constraint integrator_proced_webserv_fk foreign key (seq_webservice) references integrator_webservice(seq)
);

create table integrator_proced_param
(
      seq               number(10) not null,
      name				varchar2(1000),
	  value				varchar2(1000),
	  seq_procedure		number(10) not null,
	  param_order		number(5),
      constraint integrator_proced_param_pk primary key (seq),
	  constraint integrator_proced_param_fk foreign key (seq_procedure) references integrator_procedure(seq)
);

create table integrator_log
(
	  seq              number(10) not null,
      seq_integration  number(10) not null,
      log_date         date not null,
      log_key       varchar2(50),
      integrated_file  clob,
	  return_file	   clob,
      init_date        date,
      end_date         date,
      log_status       varchar2(100) not null,
	  log        	clob,
	  tries			number(5),
	  last_try_date	  date,
	  constraint integrator_log_pk primary key (seq),
      constraint integrator_log_integration_fk foreign key (seq_integration) references integrator_integration(seq)
);

create index log_key_index on integrator_log(log_key);

create sequence integrator_integration_seq
start with 1
increment by 1
nocache
nocycle;

create sequence integrator_segment_seq
start with 1
increment by 1
nocache
nocycle;

create sequence integrator_field_seq
start with 1
increment by 1
nocache
nocycle;

create sequence integrator_log_seq
start with 1
increment by 1
nocache
nocycle;