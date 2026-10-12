--------------------------------------------------------
--  Constraints for Table UNITSHUB_SEC_GROUPS
--------------------------------------------------------

  ALTER TABLE "INAP"."UNITSHUB_SEC_GROUPS" ADD CHECK (Is_System IN ('Y','N')) ENABLE;
  ALTER TABLE "INAP"."UNITSHUB_SEC_GROUPS" MODIFY ("GROUP_NAME" NOT NULL ENABLE);
