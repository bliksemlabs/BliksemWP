/* Copyright 2013 Bliksem Labs. See the LICENSE file at the top-level directory of this distribution and at https://github.com/bliksemlabs/rrrr/. */

/* tdata.c : handles memory mapped data file containing transit timetable etc. */
#include "tdata.h" // make sure it works alone
#include "windows.h"


#include <fcntl.h>
/*#include <sys/mman.h>*/
#include <sys/stat.h>
#include <string.h>
#include <stddef.h>
#include <stdio.h>
#include <fcntl.h>
#include <io.h>
//#include <fileapi.h>


#include "stdbool.h"
#include "config.h"
#include "util.h"

#ifdef RRRR_REALTIME
#include "gtfs-realtime.pb-c.h"
#endif


WINBASEAPI LPVOID WINAPI MapViewOfFileFromApp(HANDLE, ULONG, ULONG64, SIZE_T);
WINBASEAPI HANDLE WINAPI CreateFileMappingFromApp(HANDLE, LPSECURITY_ATTRIBUTES, ULONG, ULONG64, LPCWSTR);
WINBASEAPI BOOL WINAPI UnmapViewOfFile(LPCVOID);
#define osCreateFile ((HANDLE(WINAPI*)(LPCWSTR,DWORD,DWORD,DWORD, LPCREATEFILE2_EXTENDED_PARAMETERS))(HANDLE)CreateFile2)
#define osCreateFileMappingFromApp ((HANDLE(WINAPI*)(HANDLE,LPSECURITY_ATTRIBUTES,ULONG,ULONG64,LPCWSTR))(HANDLE)CreateFileMappingFromApp)
#define osMapViewOfFileFromApp ((LPVOID(WINAPI*)(HANDLE,ULONG,ULONG64, SIZE_T))(LPVOID)MapViewOfFileFromApp)
#define osUnmapViewOfFile ((BOOL(WINAPI*)(LPCVOID))(BOOL)UnmapViewOfFile)
#define osCloseHandle ((BOOL(WINAPI*)(HANDLE))(BOOL)CloseHandle)
#define osGetLastError ((DWORD(WINAPI*)(VOID))(DWORD)GetLastError)


// file-visible struct
typedef struct tdata_header tdata_header_t;
struct tdata_header {
    char version_string[8]; // should read "TTABLEV3"
    uint64_t calendar_start_time;
    calendar_t dst_active;
    uint32_t n_stops;
    uint32_t n_stop_attributes;
    uint32_t n_stop_coords;
    uint32_t n_routes;
    uint32_t n_route_stops;
    uint32_t n_route_stop_attributes;
    uint32_t n_stop_times;
    uint32_t n_trips;
    uint32_t n_stop_routes;
    uint32_t n_transfer_target_stops;
    uint32_t n_transfer_dist_meters;
    uint32_t n_trip_active;
    uint32_t n_route_active;
    uint32_t n_platformcodes;
    uint32_t n_stop_names; /* length of the object in bytes */
    uint32_t n_stop_nameidx;
    uint32_t n_agency_ids;
    uint32_t n_agency_names;
    uint32_t n_agency_urls;
    uint32_t n_headsigns; /* length of the object in bytes */
    uint32_t n_route_shortnames;
    uint32_t n_productcategories;
    uint32_t n_route_ids;
    uint32_t n_stop_ids;
    uint32_t n_trip_ids;
    uint32_t loc_stops;
    uint32_t loc_stop_attributes;
    uint32_t loc_stop_coords;
    uint32_t loc_routes;
    uint32_t loc_route_stops;
    uint32_t loc_route_stop_attributes;
    uint32_t loc_stop_times;
    uint32_t loc_trips;
    uint32_t loc_stop_routes;
    uint32_t loc_transfer_target_stops;
    uint32_t loc_transfer_dist_meters;
    uint32_t loc_trip_active;
    uint32_t loc_route_active;
    uint32_t loc_platformcodes;
    uint32_t loc_stop_names;
    uint32_t loc_stop_nameidx;
    uint32_t loc_agency_ids;
    uint32_t loc_agency_names;
    uint32_t loc_agency_urls;
    uint32_t loc_headsigns;
    uint32_t loc_route_shortnames;
    uint32_t loc_productcategories;
    uint32_t loc_route_ids;
    uint32_t loc_stop_ids;
    uint32_t loc_trip_ids;
};

char *tdata_route_id_for_index(tdata_t *td, uint32_t route_index) {
    if (route_index == NONE) return "NONE";
    return td->route_ids + (td->route_ids_width * route_index);
}

char *tdata_stop_id_for_index(tdata_t *td, uint32_t stop_index) {
    return td->stop_ids + (td->stop_ids_width * stop_index);
}

uint8_t *tdata_stop_attributes_for_index(tdata_t *td, uint32_t stop_index) {
    return td->stop_attributes + stop_index;
}

char *tdata_trip_id_for_index(tdata_t *td, uint32_t trip_index) {
    return td->trip_ids + (td->trip_ids_width * trip_index);
}

char *tdata_trip_id_for_route_trip_index(tdata_t *td, uint32_t route_index, uint32_t trip_index) {
    return td->trip_ids + (td->trip_ids_width * (td->routes[route_index].trip_ids_offset + trip_index));
}

char *tdata_agency_id_for_index(tdata_t *td, uint32_t agency_index) {
    return td->agency_ids + (td->agency_ids_width * agency_index);
}

char *tdata_agency_name_for_index(tdata_t *td, uint32_t agency_index) {
    return td->agency_names + (td->agency_names_width * agency_index);
}

char *tdata_agency_url_for_index(tdata_t *td, uint32_t agency_index) {
    return td->agency_urls + (td->agency_urls_width * agency_index);
}

char *tdata_headsign_for_offset(tdata_t *td, uint32_t headsign_offset) {
    return td->headsigns + headsign_offset;
}

char *tdata_route_shortname_for_index(tdata_t *td, uint32_t route_shortname_index) {
    return td->route_shortnames + (td->route_shortnames_width * route_shortname_index);
}

char *tdata_productcategory_for_index(tdata_t *td, uint32_t productcategory_index) {
    return td->productcategories + (td->productcategories_width * productcategory_index);
}

char *tdata_stop_name_for_index(tdata_t *td, uint32_t stop_index) {
    switch (stop_index) {
    case NONE :
        return "NONE";
    case ONBOARD :
        return "ONBOARD";
    default :
        return td->stop_names + td->stop_nameidx[stop_index];
    }
}

char *tdata_platformcode_for_index(tdata_t *td, uint32_t stop_index) {
    switch (stop_index) {
    case NONE :
        return NULL;
    case ONBOARD :
        return NULL;
    default :
        return td->platformcodes + (td->platformcodes_width * stop_index);
    }
}

uint32_t tdata_stopidx_by_stop_name(tdata_t *td, char* stop_desc, uint32_t start_index) {
	uint32_t stop_index;
    for (stop_index = start_index; stop_index < td->n_stops; stop_index++) {
        if (strcasestr(td->stop_names + td->stop_nameidx[stop_index], stop_desc)) {
            return stop_index;
        }
    }
    return NONE;
}

uint32_t tdata_stopidx_by_stop_id(tdata_t *td, char* stop_id, uint32_t start_index) {
	uint32_t stop_index;
    for (stop_index = start_index; stop_index < td->n_stops; stop_index++) {
        if (strcasestr(td->stop_ids + (td->stop_ids_width * stop_index), stop_id)) {
            return stop_index;
        }
    }
    return NONE;
}

uint32_t tdata_routeidx_by_route_id(tdata_t *td, char* route_id, uint32_t start_index) {
	uint32_t route_index;
    for (route_index = start_index; route_index < td->n_routes; route_index++) {
        if (strcasestr(td->route_ids + (td->route_ids_width * route_index), route_id)) {
            return route_index;
        }
    }
    return NONE;
}

char *tdata_trip_ids_for_route(tdata_t *td, uint32_t route_index) {
    route_t route = (td->routes)[route_index];
    uint32_t char_offset = route.trip_ids_offset * td->trip_ids_width;
    return td->trip_ids + char_offset;
}

calendar_t *tdata_trip_masks_for_route(tdata_t *td, uint32_t route_index) {
    route_t route = (td->routes)[route_index];
    return td->trip_active + route.trip_ids_offset;
}

char *tdata_headsign_for_route(tdata_t *td, uint32_t route_index) {
	route_t route;
	if (route_index == NONE) return "NONE";
	route = (td->routes)[route_index];
    return td->headsigns + route.headsign_offset;
}

char *tdata_shortname_for_route(tdata_t *td, uint32_t route_index) {
	route_t route;
    if (route_index == NONE) return "NONE";
    route = (td->routes)[route_index];
    return td->route_shortnames + (td->route_shortnames_width * route.shortname_index);
}

char *tdata_productcategory_for_route(tdata_t *td, uint32_t route_index) {
	route_t route;
    if (route_index == NONE) return "NONE";
    route = (td->routes)[route_index];
    return td->productcategories + (td->productcategories_width * route.productcategory_index);
}

char *tdata_agency_id_for_route(tdata_t *td, uint32_t route_index) {
	route_t route;
	if (route_index == NONE) return "NONE";
    route = (td->routes)[route_index];
    return td->agency_ids + (td->agency_ids_width * route.agency_index);
}

char *tdata_agency_name_for_route(tdata_t *td, uint32_t route_index) {
	route_t route;
	if (route_index == NONE) return "NONE";
    route = (td->routes)[route_index];
    return td->agency_names + (td->agency_names_width * route.agency_index);
}

char *tdata_agency_url_for_route(tdata_t *td, uint32_t route_index) {
	route_t route;
    if (route_index == NONE) return "NONE";
    route = (td->routes)[route_index];
    return td->agency_urls + (td->agency_urls_width * route.agency_index);
}

latlon_t *tdata_location_for_index(tdata_t *td, uint32_t stop_index) {
	switch (stop_index) {
	case NONE:
		return NULL;
	case ONBOARD:
		return NULL;
	default:
		return &td->stop_coords[stop_index];
	}
}

void tdata_check_coherent (tdata_t *tdata) {
	uint32_t s, r;
    /* Check that all lat/lon look like valid coordinates. */
    float min_lat = -55.0; // farther south than Ushuaia, Argentina
    float max_lat = +70.0; // farther north than Tromsø and Murmansk
    float min_lon = -180.0;
    float max_lon = +180.0;
    bool found_reverse;
	int n_transfers_checked = 0;
	uint32_t stop_index_from;
	printf("checking tdata coherency...\n");
    for (s = 0; s < tdata->n_stops; ++s) {
        latlon_t ll = tdata->stop_coords[s];
        if (ll.lat < min_lat || ll.lat > max_lat || ll.lon < min_lon || ll.lon > max_lon) {
            printf ("stop lat/lon out of range: lat=%f, lon=%f \n", ll.lat, ll.lon);
        }
    }
    /* Check that all timedemand types start at 0 and consist of monotonically increasing times. */
    for (r = 0; r < tdata->n_routes; ++r) {
        route_t route = tdata->routes[r];
        trip_t *trips = tdata->trips + route.trip_ids_offset;
        int n_nonincreasing_trips = 0;
		int t;
        for (t = 0; t < route.n_trips; ++t) {
            trip_t trip = trips[t];
            stoptime_t *prev_st = NULL;
			int s;
            for (s = 0; s < route.n_stops; ++s) {
                stoptime_t *st = tdata->stop_times + trip.stop_times_offset + s;
                if (s == 0 && st->arrival != 0) printf ("timedemand type begins at %d,%d not 0.\n", st->arrival, st->departure);
                if (st->departure < st->arrival) printf ("departure before arrival at route %d, trip %d, stop %d.\n", r, t, s);
                if (prev_st != NULL) {
                    if (st->arrival < prev_st->departure) {
                        // printf ("negative travel time arriving at route %d, trip %d, stop %d.\n", r, t, s);
                        // printf ("(%d, %d) -> (%d, %d)\n", prev_st->arrival, prev_st->departure, st->arrival, st->departure);
                        n_nonincreasing_trips += 1;
                    } // there are also lots of 0 travel times...
                }
                prev_st = st;
            }
        }
        if (n_nonincreasing_trips > 0) printf ("route %d has %d trips with negative travel times\n", r, n_nonincreasing_trips);
    }
    /* Check that all transfers are symmetric. */
    for (stop_index_from = 0; stop_index_from < tdata->n_stops; ++stop_index_from) {
        /* Iterate over all transfers going out of this stop */
        uint32_t t  = tdata->stops[stop_index_from    ].transfers_offset;
        uint32_t tN = tdata->stops[stop_index_from + 1].transfers_offset;
        for ( ; t < tN ; ++t) {
			uint32_t u, uN;
			uint32_t stop_index_to = tdata->transfer_target_stops[t];
            uint32_t forward_distance = tdata->transfer_dist_meters[t] << 4; // actually in units of 16 meters
            if (stop_index_to == stop_index_from) printf ("loop transfer from/to stop %d.\n", stop_index_from);
            /* Find the reverse transfer (stop_index_to -> stop_index_from) */
            found_reverse = false;
            u  = tdata->stops[stop_index_to    ].transfers_offset;
            uN = tdata->stops[stop_index_to + 1].transfers_offset;
            for ( ; u < uN ; ++u) {
                n_transfers_checked += 1;
                if (tdata->transfer_target_stops[u] == stop_index_from) {
                    /* this is the same transfer in reverse */
                    uint32_t reverse_distance = tdata->transfer_dist_meters[u] << 4;
                    if (reverse_distance != forward_distance) {
                        printf ("transfer from %d to %d is not symmetric. "
                                "forward distance is %d, reverse distance is %d.\n",
                                stop_index_from, stop_index_to, forward_distance, reverse_distance);
                    }
                    found_reverse = true;
                    break;
                }
            }
            if ( ! found_reverse) printf ("transfer from %d to %d does not have an equivalent reverse transfer.\n", stop_index_from, stop_index_to);
        }
    }
    printf ("checked %d transfers for symmetry.\n", n_transfers_checked);
}

/* Map an input file into memory and reconstruct pointers to its contents. */

HANDLE tdata_load(char *filename, tdata_t *td) {
	DWORD dwFlagsAndAttributes = 0;
	struct stat st;
	wchar_t* wideName;
	size_t nameLen;
	HANDLE fd, mapping = NULL;
	char *b;
	size_t convertedChars = 0;
	tdata_header_t *header;

	nameLen = strlen(filename);
	wideName = malloc((nameLen + 1) * sizeof(wchar_t));
	mbstowcs_s(&convertedChars, wideName, nameLen + 1, filename, nameLen);
	fd = osCreateFile(wideName, GENERIC_READ, FILE_SHARE_READ, OPEN_EXISTING, 0);
	free(wideName);
    if (stat(filename, &st) == -1)
        die("could not stat input file");

	mapping = (HANDLE)osCreateFileMappingFromApp(fd, NULL, PAGE_READONLY, st.st_size, NULL);
	td->base = (char *)osMapViewOfFileFromApp(mapping, FILE_MAP_READ, 0, st.st_size);
    td->size = st.st_size;
    if (td->base == (void*)(-1))
        die("could not map input file");

    b = (char *) td->base;
    header = (tdata_header_t *) b;
	
    if( strncmp("TTABLEV3", header->version_string, 8) )
        die("the input file does not appear to be a timetable or is of the wrong version");
	OutputDebugString(L"td->calendar_start_time\n");

    /* Check if this works properly in Visual Studio */
	td->calendar_start_time = header->calendar_start_time;
	td->dst_active = header->dst_active;
    load_mmap (b, stops, stop_t);
    load_mmap (b, stop_attributes, uint8_t);
    load_mmap (b, stop_coords, latlon_t);
    load_mmap (b, routes, route_t);
    load_mmap (b, route_stops, uint32_t);
    load_mmap (b, route_stop_attributes, uint8_t);
    load_mmap (b, stop_times, stoptime_t);
    load_mmap (b, trips, trip_t);
    load_mmap (b, stop_routes, uint32_t);
    load_mmap (b, transfer_target_stops, uint32_t);
    load_mmap (b, transfer_dist_meters, uint8_t);
    load_mmap (b, trip_active, calendar_t);
    load_mmap (b, route_active, calendar_t);
    load_mmap (b, headsigns, char);
    load_mmap (b, stop_names, char);
    load_mmap (b, stop_nameidx, uint32_t);

    load_mmap_string (b, platformcodes);
    load_mmap_string (b, stop_ids);
    load_mmap_string (b, trip_ids);
    load_mmap_string (b, agency_ids);
    load_mmap_string (b, agency_names);
    load_mmap_string (b, agency_urls);
    load_mmap_string (b, route_shortnames);
    load_mmap_string (b, route_ids);
    load_mmap_string (b, productcategories);

#ifdef REALTIME
    td->alerts = NULL;
#endif
    // This is probably a bit slow and is not strictly necessary, but does page in all the timetable entries.
    tdata_check_coherent(td);
    // D tdata_dump(td);
	return mapping;
}

void tdata_close(tdata_t *td, HANDLE mapping) {
	// t.otdou
    //munmap(td->base, td->size);
	if (!osUnmapViewOfFile(td->base)) {
		die(osGetLastError());
	}

	if (!osCloseHandle(mapping)) {
		die(osGetLastError());
	}
}

// TODO should pass pointer to tdata?
uint32_t *tdata_stops_for_route(tdata_t *td, uint32_t route) {
    route_t route0 = td->routes[route];
    return td->route_stops + route0.route_stops_offset;
}

uint8_t *tdata_stop_attributes_for_route(tdata_t *td, uint32_t route) {
    route_t route0 = td->routes[route];
    return td->route_stop_attributes + route0.route_stops_offset;
}

uint32_t tdata_routes_for_stop(tdata_t *td, uint32_t stop, uint32_t **routes_ret) {
    stop_t stop0 = td->stops[stop];
    stop_t stop1 = td->stops[stop + 1];
    *routes_ret = td->stop_routes + stop0.stop_routes_offset;
    return stop1.stop_routes_offset - stop0.stop_routes_offset;
}

// TODO used only in dumping routes; trip_index is not used in the expression?
stoptime_t *tdata_timedemand_type(tdata_t *td, uint32_t route_index, uint32_t trip_index) {
    return td->stop_times + td->trips[td->routes[route_index].trip_ids_offset + trip_index].stop_times_offset;
}

trip_t *tdata_trips_for_route (tdata_t *td, uint32_t route_index) {
    return td->trips + td->routes[route_index].trip_ids_offset;
}
#if 0
uint8_t *tdata_trip_attributes_for_route (tdata_t *td, uint32_t route_index) {
    return td->trip_attributes + td->routes[route_index].trip_ids_offset;
}
#endif

#ifdef RRRR_REALTIME
/* Signed delay of the specified trip, in seconds. */
float tdata_delay_min (tdata_t *td, uint32_t route_index, uint32_t trip_index) {
    trip_t *trips = tdata_trips_for_route(td, route_index);
    return RTIME_TO_SEC_SIGNED(trips[trip_index].realtime_delay) / 60.0;
}
#endif

void tdata_dump_route(tdata_t *td, uint32_t route_idx, uint32_t trip_idx) {
	printf("\nNot implemented in Windows!");
    /*uint32_t *stops = tdata_stops_for_route(td, route_idx);
    route_t route = td->routes[route_idx];
    printf("\nRoute details for %s %s %s '%s %s' [%d] (n_stops %d, n_trips %d)\n", tdata_agency_name_for_route(td, route_idx),
        tdata_agency_id_for_route(td, route_idx), tdata_agency_url_for_route(td, route_idx),
        tdata_shortname_for_route(td, route_idx), tdata_headsign_for_route(td, route_idx), route_idx, route.n_stops, route.n_trips);
    printf("tripid, stop sequence, stop name (index), departures  \n");
    for (uint32_t ti = (trip_idx == NONE ? 0 : trip_idx); ti < (trip_idx == NONE ? route.n_trips : trip_idx + 1); ++ti) {
        // TODO should this really be a 2D array ?
#ifdef WIN32
		stoptime_t (*times)[route.n_stops] = (void*) tdata_timedemand_type(td, route_idx, ti);
#else
        stoptime_t (*times)[route.n_stops] = (void*) tdata_timedemand_type(td, route_idx, ti);
#endif
        printf("%s ", tdata_trip_id_for_index(td, route.trip_ids_offset + ti));
        for (uint32_t si = 0; si < route.n_stops; ++si) {
            char *stop_id = tdata_stop_name_for_index (td, stops[si]);
            printf("%4d %35s [%06d] : %s", si, stop_id, stops[si], timetext(times[0][si].departure + td->trips[route.trip_ids_offset + ti].begin_time + RTIME_ONE_DAY));
         }
         printf("\n");
    }
    printf("\n");*/
}

#if 0
void tdata_dump(tdata_t *td) {
    printf("\nCONTEXT\n"
           "n_stops: %d\n"
           "n_routes: %d\n", td->n_stops, td->n_routes);
    printf("\nSTOPS\n");
    for (uint32_t i = 0; i < td->n_stops; i++) {
        printf("stop %d at lat %f lon %f\n", i, td->stop_coords[i].lat, td->stop_coords[i].lon);
        stop_t s0 = td->stops[i];
        stop_t s1 = td->stops[i+1];
        uint32_t j0 = s0.stop_routes_offset;
        uint32_t j1 = s1.stop_routes_offset;
        uint32_t j;
        printf("served by routes ");
        for (j=j0; j<j1; ++j) {
            printf("%d ", td->stop_routes[j]);
        }
        printf("\n");
    }
    printf("\nROUTES\n");
    for (uint32_t i = 0; i < td->n_routes; i++) {
        printf("route %d\n", i);
        printf("having trips %d\n", td->routes[i].n_trips);
        route_t r0 = td->routes[i];
        route_t r1 = td->routes[i+1];
        uint32_t j0 = r0.route_stops_offset;
        uint32_t j1 = r1.route_stops_offset;
        uint32_t j;
        printf("serves stops ");
        for (j=j0; j<j1; ++j) {
            printf("%d ", td->route_stops[j]);
        }
        printf("\n");
    }
    printf("\nSTOPIDS\n");
    for (uint32_t i = 0; i < td->n_stops; i++) {
        printf("stop %03d has id %s \n", i, tdata_stop_name_for_index(td, i));
    }
#if 0
    printf("\nROUTEIDS, TRIPIDS\n");
    for (uint32_t i = 0; i < td->n_routes; i++) {
        printf("route %03d has id %s and first trip id %s \n", i,
            tdata_route_desc_for_index(td, i),
            tdata_trip_ids_for_route(td, i));
    }
#endif
}
#endif

#ifdef RRRR_REALTIME
/*
  Decodes the GTFS-RT message of lenth len in buffer buf, extracting vehicle position messages
  and using the delay extension (1003) to update RRRR's per-trip delay information.
*/
void tdata_apply_gtfsrt (tdata_t *tdata, RadixTree *tripid_index, uint8_t *buf, size_t len) {
    TransitRealtime__FeedMessage *msg;
    msg = transit_realtime__feed_message__unpack (NULL, len, buf);
    if (msg == NULL) {
        fprintf (stderr, "error unpacking incoming gtfs-rt message\n");
        return;
    }
    printf("Received feed message with %zu entities.\n", msg->n_entity);
    for (size_t e = 0; e < msg->n_entity; ++e) {
        TransitRealtime__FeedEntity *entity = msg->entity[e];
        if (entity == NULL) goto cleanup;
        // printf("  entity %d has id %s\n", e, entity->id);
        TransitRealtime__VehiclePosition *vehicle = entity->vehicle;
        if (vehicle == NULL) goto cleanup;
        TransitRealtime__TripDescriptor *trip = vehicle->trip;
        if (trip == NULL) goto cleanup;
        char *trip_id = trip->trip_id;

        int32_t delay_sec = 0;
        if (trip->schedule_relationship == TRANSIT_REALTIME__TRIP_DESCRIPTOR__SCHEDULE_RELATIONSHIP__CANCELED) {
            delay_sec = CANCELED;
        } else {
            TransitRealtime__OVapiVehiclePosition *ovapi_vehicle_position = vehicle->ovapi_vehicle_position;
            if (ovapi_vehicle_position == NULL) printf ("    entity contains no delay message.\n");
            else delay_sec = ovapi_vehicle_position->delay;
            if (abs(delay_sec) > 60 * 120) {
                printf ("    filtering out extreme delay of %d sec.\n", delay_sec);
                delay_sec = 0;
            }
        }

        /* Apply delay. */
        uint32_t trip_index = rxt_find (tripid_index, trip_id);
        if (trip_index == RADIX_TREE_NONE) {
            printf ("    trip id was not found in the radix tree.\n");
        } else {
            // printf ("    trip_id %s, trip number %d, applying delay of %d sec.\n", trip_id, trip_index, delay_sec);
            trip_t *trip = tdata->trips + trip_index;
            trip->realtime_delay = SEC_TO_RTIME(delay_sec);
        }
    }
    cleanup:
    transit_realtime__feed_message__free_unpacked (msg, NULL);
}

void tdata_clear_gtfsrt (tdata_t *tdata) {
    /* If we had the total number of trips nested loops would not be necessary. */
    uint32_t r = 0;
    for ( ; r < tdata->n_routes; ++r) {
        route_t route = tdata->routes[r];
        trip_t *trips = tdata_trips_for_route(tdata, r);
        for (int t = 0; t < route.n_trips; ++t) {
            trips[t].realtime_delay = 0;
        }
    }
}

void tdata_apply_gtfsrt_file (tdata_t *tdata, RadixTree *tripid_index, char *filename) {
    int fd = open(filename, O_RDONLY);
    if (fd == -1) die("Could not find GTFS_RT input file.\n");
    struct stat st;
    if (stat(filename, &st) == -1) die("Could not stat GTFS_RT input file.\n");
    uint8_t *buf = mmap(NULL, st.st_size, PROT_READ, MAP_SHARED, fd, 0);
    if (buf == MAP_FAILED) die("Could not map GTFS-RT input file.\n");
    tdata_apply_gtfsrt (tdata, tripid_index, buf, st.st_size);
    munmap (buf, st.st_size);
}

void tdata_apply_gtfsrt_alerts (tdata_t *tdata, RadixTree *routeid_index, RadixTree *stopid_index, RadixTree *tripid_index, uint8_t *buf, size_t len) {
    TransitRealtime__FeedMessage *msg = transit_realtime__feed_message__unpack (NULL, len, buf);
    size_t e = 0;
    if (msg == NULL) {
        fprintf (stderr, "error unpacking incoming gtfs-rt message\n");
        return;
    }

    printf("Received feed message with %zu entities.\n", msg->n_entity);
    for (; e < msg->n_entity; ++e) {
        TransitRealtime__FeedEntity *entity = msg->entity[e];
        size_t ie = 0;
        if (entity == NULL) goto cleanup;
        // printf("  entity %d has id %s\n", e, entity->id);
        TransitRealtime__Alert *alert = entity->alert;
        if (alert == NULL) goto cleanup;
        for (; ie < alert->n_informed_entity; ++ie) {
            TransitRealtime__EntitySelector *informed_entity = alert->informed_entity[ie];
            if (!informed_entity) continue;

            if (informed_entity->route_id) {
                uint32_t route_index = rxt_find (routeid_index, informed_entity->route_id);
                if (route_index == RADIX_TREE_NONE) {
                     printf ("    route id was not found in the radix tree.\n");
                }
                memcpy (informed_entity->route_id, &route_index, sizeof(route_index));
            }

            if (informed_entity->stop_id) {
                uint32_t stop_index = rxt_find (stopid_index, informed_entity->stop_id);
                if (stop_index == RADIX_TREE_NONE) {
                     printf ("    stop id was not found in the radix tree.\n");
                }
                memcpy (informed_entity->stop_id, &stop_index, sizeof(stop_index));
            }

            if (informed_entity->trip && informed_entity->trip->trip_id) {
                uint32_t trip_index = rxt_find (tripid_index, informed_entity->trip->trip_id);
                if (trip_index == RADIX_TREE_NONE) {
                    printf ("    trip id was not found in the radix tree.\n");
                }
                memcpy (informed_entity->trip->trip_id, &trip_index, sizeof(trip_index));
            }
        }
    }

    tdata->alerts = msg;
    return;

    cleanup:
    transit_realtime__feed_message__free_unpacked (msg, NULL);
}

void tdata_clear_gtfsrt_alerts (tdata_t *tdata) {
    if (tdata->alerts) {
        transit_realtime__feed_message__free_unpacked (tdata->alerts, NULL);
        tdata->alerts = NULL;
    }
}

void tdata_apply_gtfsrt_alerts_file (tdata_t *tdata, RadixTree *routeid_index, RadixTree *stopid_index, RadixTree *tripid_index, char *filename) {
    int fd = open(filename, O_RDONLY);
    if (fd == -1) die("Could not find GTFS_RT input file.\n");
    struct stat st;
    if (stat(filename, &st) == -1) die("Could not stat GTFS_RT input file.\n");
    uint8_t *buf = mmap(NULL, st.st_size, PROT_READ, MAP_SHARED, fd, 0);
    if (buf == MAP_FAILED) die("Could not map GTFS-RT input file.\n");
    tdata_apply_gtfsrt_alerts (tdata, routeid_index, stopid_index, tripid_index, buf, st.st_size);
    munmap (buf, st.st_size);
}
#endif
// tdata_get_route_stops

/* optional stop ids, names, coordinates... */
