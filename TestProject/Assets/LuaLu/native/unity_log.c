#include "unity_log.h"

#ifdef __cplusplus
extern "C" {
#endif
    
unity_log_func unity_log;
    
void set_unity_log_func(unity_log_func fp) {
    unity_log = fp;
    unity_log("Unity log function is set in LuaLu plugin");
}

#ifdef __cplusplus
}
#endif