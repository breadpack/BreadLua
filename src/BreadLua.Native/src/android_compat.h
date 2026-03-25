/*
 * Android 32-bit NDK compatibility header.
 * Overrides Lua's l_fseek/l_ftell to avoid fseeko/ftello
 * which are not available on armv7 NDK without _GNU_SOURCE.
 */
#ifndef ANDROID_COMPAT_H
#define ANDROID_COMPAT_H

#ifdef __ANDROID__
#include <stdio.h>
#define l_fseek(f,o,w)  fseek(f,o,w)
#define l_ftell(f)       ftell(f)
#define l_seeknum        long
#endif

#endif /* ANDROID_COMPAT_H */
