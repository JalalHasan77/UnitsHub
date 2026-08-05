--------------------------------------------------------
--  Constraints for Table UNITSHUBX_GROUPS
--------------------------------------------------------

  ALTER TABLE "INAP"."UNITSHUBX_GROUPS" ADD CHECK (Is_System IN ('Y','N')) ENABLE;
  ALTER TABLE "INAP"."UNITSHUBX_GROUPS" MODIFY ("GROUP_NAME" NOT NULL ENABLE);
