import type { TimetableResponse } from "$lib/types/db_raw_types";

export const colours = new Set([
  "bg-red-400",
  "bg-orange-400",
  "bg-amber-300",
  "bg-yellow-200",
  "bg-lime-500",
  "bg-green-400",
  "bg-emerald-400",
  "bg-teal-400",
  "bg-cyan-400",
  "bg-sky-500",
  "bg-blue-500",
  "bg-indigo-300",
  "bg-violet-400",
  "bg-purple-400",
  "bg-fuchsia-300",
  "bg-pink-400",
  "bg-rose-300",
  "bg-slate-300",
  "bg-olive-400",
  "bg-mauve-400",
  "bg-taupe-300",
  "bg-stone-400",
  "bg-teal-200",
  "bg-red-100",
]);
// Returns semester numbers as "Special term I" and "Special term II"
// if 3 and 4
export function format_semester_name(number: number): string {
  let semester_name = "";
  switch (number) {
    case 1:
    case 2:
      semester_name = `Semester ${number}`;
      break;
    case 3:
    case 4:
      semester_name = `Special Term ${number - 2}`;
      break;
    default:
      break;
  }

  return semester_name;
}

export function format_AY_name(ay_string: string): string {
  const year_split = ay_string.split("-");
  return `AY${year_split[0]}/${year_split[1]}`;
}

export function format_created_time(time_string: string): string {
  const date = new Date(time_string);
  return date.toLocaleString();
}

export function get_random_colour(): string {
  let colour = "";
  const colour_array_iter = colours.values();
  for (
    let index = -1;
    index < Math.floor(Math.random() * colours.size);
    index++
  ) {
    colour = colour_array_iter.next().value!;
  }
  return colour;
}

export function get_randomised_colour(
  timetable_info: TimetableResponse[],
): string {
  const currentSelectedColours = new Set<string>();

  for (const tt of timetable_info) {
    for (const lesson of tt.metaData) {
      currentSelectedColours.add(lesson.colour);
    }
  }

  const unused_colours = colours.difference(currentSelectedColours);
  const unused_colours_iter = colours.values();
  let colour = "";
  if (unused_colours.size != 0) {
    for (
      let index = -1;
      index < Math.floor(Math.random() * unused_colours.size);
      index++
    ) {
      colour = unused_colours_iter.next().value!;
    }
    return colour;
  }

  const colour_array_iter = colours.values();
  for (
    let index = -1;
    index < Math.floor(Math.random() * colours.size);
    index++
  ) {
    colour = colour_array_iter.next().value!;
  }
  return colour;
}
