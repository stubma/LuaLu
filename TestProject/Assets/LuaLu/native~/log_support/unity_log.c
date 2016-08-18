#include "unity_log.h"

#ifdef __cplusplus
extern "C" {
#endif
    
static unity_log_func unity_log_fp;
    
void set_unity_log_func(unity_log_func fp) {
    unity_log_fp = fp;
    unity_log("Unity log function is set in LuaLu plugin");
}

void unity_log(const char* s) {
	unity_log_fp(s);
}

#ifdef __cplusplus
}
#endif