#ifndef __UNITY_LOG_H__
#define __UNITY_LOG_H__

#ifndef UNITY_PLUGIN_API
#define UNITY_PLUGIN_API extern
#endif

#ifdef __cplusplus
extern "C" {
#endif
    
// log function prototype
typedef void (*unity_log_func)(const char *);
extern unity_log_func unity_log;
    
// function to set log function from unity side
UNITY_PLUGIN_API void set_unity_log_func(unity_log_func fp);
    
#ifdef __cplusplus
}
#endif

#endif /* __UNITY_LOG_H__ */
