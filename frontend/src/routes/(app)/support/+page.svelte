<script lang="ts">
  import type { PageProps } from "./$types";
  import donate from "$lib/assets/mods_tgt_qr.png?enhanced";
  let { data }: PageProps = $props();
</script>

<p class="mt-2 text-xl font-semibold">Support ModsTogether!</p>
<hr class="my-2 h-px border-0 bg-gray-200" />

<div class="text-wrap">
  ModsTogether is a free service that allows you to plan your timetables with
  your friends. We aim to keep this service free forever, but running the
  databases and various hosting services requires some money.
</div>

<div class="text-wrap py-2">
  If you think ModsTogether has helped you in any way and you would like to
  <a
    href="https://buymeacoffee.com/modstgt"
    target="_blank"
    class="link link-primary link-hover"
  >
    buy us a coffee</a
  >, please feel free to! Any amount is appreciated, please donate within your
  means. ❤️
</div>

<div class="flex align-middle w-full justify-center">
  <a href="https://www.buymeacoffee.com/modstgt"
    ><img
      src="https://img.buymeacoffee.com/button-api/?text=Buy us a coffee&emoji=&slug=modstgt&button_colour=FFDD00&font_colour=000000&font_family=Cookie&outline_colour=000000&coffee_colour=ffffff"
    /></a
  >
</div>

<div class="divider">OR</div>

<div class="flex items-center justify-center">
  <enhanced:img class="w-64" src={donate} />
</div>
