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
    
// function to set log function from unity side
UNITY_PLUGIN_API void set_unity_log_func(unity_log_func fp);

// unity log
UNITY_PLUGIN_API void unity_log(const char* s);
    
#ifdef __cplusplus
}
#endif

#endif /* __UNITY_LOG_H__ */
