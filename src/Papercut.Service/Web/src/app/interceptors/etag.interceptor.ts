import {
  HttpInterceptorFn,
  HttpRequest,
  HttpHandlerFn,
  HttpEvent,
  HttpResponse
} from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { tap } from 'rxjs/operators';

// Using a WeakMap to store cache without memory leaks
const cache = new WeakMap<object, Map<string, { etag: string; data: any }>>();

export const etagInterceptor: HttpInterceptorFn = (
  request: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  // Only handle GET requests
  if (request.method !== 'GET') {
    return next(request);
  }

  // Get or create cache for this request
  let requestCache = cache.get(request);
  if (!requestCache) {
    requestCache = new Map();
    cache.set(request, requestCache);
  }

  const cachedResponse = requestCache.get(request.url);
  if (cachedResponse) {
    // Add If-None-Match header with the cached ETag
    request = request.clone({
      setHeaders: {
        'If-None-Match': cachedResponse.etag
      }
    });
  }

  return next(request).pipe(
    tap(event => {
      if (event instanceof HttpResponse) {
        const etag = event.headers.get('ETag');
        if (etag) {
          // Cache the response with its ETag
          requestCache.set(request.url, {
            etag,
            data: event.body
          });
        }
      }
    })
  );
}; 