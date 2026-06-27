<script lang="ts">
	import { chooseModState } from '$lib/shared/shared.svelte';

	interface BackgroundTilesProps {
		heightOfOneHourLessonPx: number;
		x_cord: number;
		y_cord: number;
		max_hours_displayed: number;
	}
	const { heightOfOneHourLessonPx, x_cord, y_cord, max_hours_displayed }: BackgroundTilesProps =
		$props();
</script>

<!-- svelte-ignore a11y_click_events_have_key_events -->
<!-- svelte-ignore a11y_no_static_element_interactions -->
<div
	onclick={() => {
		$chooseModState = {
			classNo: '',
			colour: '',
			lessonType: '',
			moduleCode: '',
			selectedTimetableId: ''
		};
	}}
	class="col-start-{x_cord + 1} row-start-{y_cord + 1} h-{heightOfOneHourLessonPx} {y_cord % 2 == 0
		? 'bg-base-100'
		: 'bg-base-200'} {y_cord === 0 ? 'border-t' : ''} {x_cord !== 5 ? 'border-r' : ''} {x_cord === 0
		? 'border-l'
		: ''} {y_cord === max_hours_displayed - 1 ? 'border-b' : ''}"
></div>
