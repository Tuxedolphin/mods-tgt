<script lang="ts">
  import type { PageProps } from "./$types";

  let { data }: PageProps = $props();
</script>

<p class="mt-2 text-xl font-semibold">I have an issue!</p>
<hr class="my-2 h-px border-0 bg-gray-200" />

<p class="">
  We're sorry to hear that you've faced an issue. You may report it using the
  following methods:
</p>

<ul class="list list-disc m-2">
  <li>
    Open an issue on <a
      class="link link-hover link-primary"
      target="_blank"
      href="https://github.com/Tuxedolphin/mods-tgt/issues">Github</a
    >
  </li>
  <li>
    Report it using <a
      href="https://forms.gle/xe9v1qxjZo28kgT66"
      class="link link-hover link-primary">Google Forms</a
    >
  </li>
</ul>

<p class="">
  Mods Together is under active development, thank you for helping us make this
  product better!
</p>
